using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FareverLogs.Uploader.Config;
using FareverLogs.Uploader.Navigation;

namespace FareverLogs.Uploader.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
#if DEBUG
    public bool IsServerPickerVisible => true;
    public List<string> Urls { get; } = ["https://fareverlogs.fly.dev/", "https://localhost:50385/"];
#else
    public bool IsServerPickerVisible => false;
    public List<string> Urls { get; } = ["https://fareverlogs.fly.dev/"];
#endif

    [ObservableProperty] private string _selectedUrl;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool   _isWaiting  = false;

    public bool IsAlreadyLoggedIn => !string.IsNullOrEmpty(_config.JwtToken);

    private readonly AppConfig          _config;
    private readonly NavigationService  _nav;
    private CancellationTokenSource?    _cts;

    public LoginViewModel(AppConfig config, NavigationService nav)
    {
        _config      = config;
        _nav         = nav;
#if DEBUG
        _selectedUrl = !string.IsNullOrEmpty(config.ServerUrl)
            ? config.ServerUrl
            : "https://fareverlogs.fly.dev/";
#else
        _selectedUrl = "https://fareverlogs.fly.dev/";
#endif
    }

    [RelayCommand]
    private async Task Login()
    {
        _cts?.Cancel();
        _cts       = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        IsWaiting  = true;
        StatusText = "Opening browser…";

        try
        {
            var port    = FindFreePort();
            var authUrl = SelectedUrl.TrimEnd('/') + $"/api/auth/login?callbackPort={port}";
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            StatusText = "Waiting for Discord login in your browser…";
            var token  = await WaitForCallbackAsync(port, _cts.Token);

            if (token is not null)
            {
                _config.JwtToken        = token;
                _config.DiscordUsername = ParseUsername(token) ?? "";
                _config.ServerUrl       = SelectedUrl;
                _config.Save();
                _nav.NavigateTo<HomeViewModel>();
            }
            else
            {
                StatusText = "Login cancelled or timed out.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Login timed out. Please try again.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsWaiting = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "";
        IsWaiting  = false;
    }

    private static async Task<string?> WaitForCallbackAsync(int port, CancellationToken ct)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        try
        {
            var ctx = await listener.GetContextAsync().WaitAsync(ct);

            const string html = """
                <html><body style="font-family:sans-serif;padding:2rem;background:#0e0f13;color:#e2e8f0">
                <h2 style="color:#38bdf8">Login successful!</h2>
                <p>You can close this tab and return to FareverLogs Uploader.</p>
                </body></html>
                """;
            var bytes = Encoding.UTF8.GetBytes(html);
            ctx.Response.ContentType     = "text/html";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, ct);
            ctx.Response.Close();

            return ExtractQueryParam(ctx.Request.Url?.Query ?? "", "token");
        }
        finally
        {
            listener.Stop();
        }
    }

    private static int FindFreePort()
    {
        var l = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static string? ExtractQueryParam(string query, string key)
    {
        query = query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }

    private static string? ParseUsername(string token)
    {
        try
        {
            var parts  = token.Split('.');
            if (parts.Length < 2) return null;
            var padded = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(padded));
            if (doc.RootElement.TryGetProperty("username", out var un))
                return un.GetString();
        }
        catch { }
        return null;
    }
}
