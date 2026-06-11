using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FareverLogs.Uploader.Config;
using FareverLogs.Uploader.Navigation;

namespace FareverLogs.Uploader.ViewModels;

public partial class FileItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public string FileName    { get; init; } = "";
    public string FilePath    { get; init; } = "";
    public string DisplayDate { get; init; } = "";
}

public sealed partial class UploadExistingViewModel : ObservableObject
{
    public ObservableCollection<FileItem> Files   { get; } = [];
    public ObservableCollection<LogEntry> Results { get; } = [];

    [ObservableProperty] private bool   _isUploading;
    [ObservableProperty] private string _statusText = "";

    private readonly AppConfig         _config;
    private readonly NavigationService _nav;

    public UploadExistingViewModel(AppConfig config, NavigationService nav)
    {
        _config = config;
        _nav    = nav;
    }

    public void ScanFiles()
    {
        Files.Clear();
        Results.Clear();
        StatusText = "";

        var dir = _config.FareverFolder;
        if (!Directory.Exists(dir))
        {
            StatusText = $"Directory not found: {dir}";
            return;
        }

        foreach (var f in Directory.EnumerateFiles(dir, "*.json")
                     .Concat(Directory.EnumerateFiles(dir, "*.json.gz"))
                     .OrderByDescending(File.GetLastWriteTime))
        {
            Files.Add(new FileItem
            {
                FileName    = Path.GetFileName(f),
                FilePath    = f,
                DisplayDate = File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm"),
            });
        }

        StatusText = Files.Count == 0 ? "No log files found." : "";
    }

    [RelayCommand]
    private void SelectAll()   { foreach (var f in Files) f.IsSelected = true; }

    [RelayCommand]
    private void ClearSelection() { foreach (var f in Files) f.IsSelected = false; }

    [RelayCommand]
    private async Task Upload()
    {
        var selected = Files.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0) { StatusText = "No files selected."; return; }

        IsUploading = true;
        StatusText  = "";

        var url     = _config.ServerUrl.TrimEnd('/') + "/";
        var handler = new HttpClientHandler();
        if (url.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var http = new HttpClient(handler) { BaseAddress = new Uri(url) };
        if (!string.IsNullOrEmpty(_config.JwtToken))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.JwtToken);

        foreach (var file in selected)
        {
            Log($"Uploading {file.FileName}…");
            var result = await FareverCompanionCombatParser.UploadFileOnceAsync(http, file.FilePath);
            if (result.Success)
                Log($"✓ {file.FileName}", result.ReportUrl);
            else
                Log($"✗ {file.FileName}: {result.ErrorMessage}");
        }

        IsUploading = false;
        StatusText  = $"Done — {selected.Count} file(s) processed.";
    }

    [RelayCommand]
    private void Back() => _nav.GoBack();

    private void Log(string text, string? url = null) =>
        Dispatcher.UIThread.Post(() =>
            Results.Add(new LogEntry($"[{DateTime.Now:HH:mm:ss}] {text}", url)));
}
