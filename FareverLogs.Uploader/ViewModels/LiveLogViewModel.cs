using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FareverLogs.Uploader.Config;
using FareverLogs.Uploader.Navigation;

namespace FareverLogs.Uploader.ViewModels;

public sealed partial class LiveLogViewModel : ObservableObject
{
    [ObservableProperty] private bool _isRunning;

    public string StartButtonLabel => IsRunning ? "Stop Live Logging" : "Start Live Logging";
    partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(StartButtonLabel));

    public ObservableCollection<LogEntry> LogMessages { get; } = [];

    private CancellationTokenSource?  _cts;
    private readonly AppConfig         _config;
    private readonly NavigationService _nav;

    public LiveLogViewModel(AppConfig config, NavigationService nav)
    {
        _config = config;
        _nav    = nav;
    }

    [RelayCommand]
    private void Toggle()
    {
        if (IsRunning) { _cts?.Cancel(); return; }

        IsRunning = true;
        _cts      = new CancellationTokenSource();
        var url   = _config.ServerUrl.TrimEnd('/') + "/";
        var token = _cts.Token;

        var logDir = _config.FareverFolder;
        Log($"Watching {logDir}");
        Log($"Uploading to {url}");

        Task.Run(async () =>
        {
            var handler = new HttpClientHandler();
            if (url.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var http = new HttpClient(handler) { BaseAddress = new Uri(url) };
            if (!string.IsNullOrEmpty(_config.JwtToken))
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _config.JwtToken);

            var parser = new FareverCompanionCombatParser(http, Log);
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await parser.ScanDirectoryAsync(logDir);
                    await Task.Delay(1000, token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                Dispatcher.UIThread.Post(() => IsRunning = false);
                Log("Stopped.");
            }
        }, token);
    }

    [RelayCommand]
    private void Back() => _nav.GoBack();

    private void Log(string message, string? url = null)
    {
        var prefix = url != null && message.EndsWith(url)
            ? $"[{DateTime.Now:HH:mm:ss}] {message[..^url.Length]}"
            : $"[{DateTime.Now:HH:mm:ss}] {message}";
        Dispatcher.UIThread.Post(() => LogMessages.Add(new LogEntry(prefix, url)));
    }
}
