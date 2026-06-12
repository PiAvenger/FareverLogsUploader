using System.Net.Http.Json;

namespace FareverLogs.Uploader;

public class FareverCompanionCombatParser
{
    public record UploadResult(bool Success, string? ReportUrl, string? ErrorMessage);

    private const string UploadEndpoint = "api/report/upload/file";

    public static async Task<UploadResult> UploadFileOnceAsync(HttpClient httpClient, string filePath)
    {
        try
        {
            using var response = await PostFileAsync(httpClient, filePath, null);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<UploadResponse>();
                var baseUrl  = httpClient.BaseAddress?.ToString().TrimEnd('/');
                return new UploadResult(true, $"{baseUrl}/report/{payload?.Id}", null);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                return new UploadResult(false, null, string.Join(", ", errors ?? []));
            }
            return new UploadResult(false, null, $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new UploadResult(false, null, ex.Message);
        }
    }

    private readonly HttpClient _httpClient;
    private readonly Action<string, string?> _log;
    private readonly Queue<string> _retryQueue = new();
    private DateTime _nextRetryAt  = DateTime.MinValue;
    private DateTime _lastScanTime = DateTime.UtcNow;

    public FareverCompanionCombatParser(HttpClient httpClient, Action<string, string?> log)
    {
        _httpClient = httpClient;
        _log        = log;
    }

    private void Log(string message) => _log(message, null);

    public async Task ScanDirectoryAsync(string directory)
    {
        var scanStart = DateTime.UtcNow;

        var newFiles = Directory
            .EnumerateFiles(directory, "*.json")
            .Concat(Directory.EnumerateFiles(directory, "*.json.gz"))
            .Where(f => File.GetCreationTimeUtc(f) > _lastScanTime)
            .OrderBy(File.GetCreationTimeUtc)
            .ToList();

        foreach (var file in newFiles)
        {
            Log($"Processing companion log: {Path.GetFileName(file)}");
            await ParseAndUploadAsync(file);
        }
        if (newFiles.Any())
        {
            _lastScanTime = scanStart;
        }
    }

    public async Task ParseAndUploadAsync(string filePath)
    {
        await UploadAsync(filePath);
        await RetryQueuedAsync();
    }

    private async Task UploadAsync(string filePath)
    {
        try
        {
            using var response = await PostFileAsync(_httpClient, filePath, Log);

            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<UploadResponse>();
                var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');
                var reportUrl = $"{baseUrl}/report/{payload?.Id}";
                _log($"Upload succeeded: {reportUrl}", reportUrl);
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                Log($"Upload rejected: {string.Join(", ", errors ?? [])}");
                return;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                Log($"Upload failed: {string.Join(", ", errors ?? ["Unauthorized"])}");
                return;
            }

            Log($"Upload failed ({(int)response.StatusCode}), queuing for retry.");
            _retryQueue.Enqueue(filePath);
            _nextRetryAt = DateTime.UtcNow.AddSeconds(30);
        }
        catch (Exception ex)
        {
            Log($"Upload failed, queuing for retry: {ex.Message}");
            _retryQueue.Enqueue(filePath);
            _nextRetryAt = DateTime.UtcNow.AddSeconds(30);
        }
    }

    private record UploadResponse(string Id);

    // Posts the file, transparently honouring a 429 by waiting the number of
    // seconds the server asks for and trying again.
    private static async Task<HttpResponseMessage> PostFileAsync(
        HttpClient httpClient, string filePath, Action<string>? log)
    {
        while (true)
        {
            await using var stream = File.OpenRead(filePath);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(stream), "file", Path.GetFileName(filePath));

            var response = await httpClient.PostAsync(UploadEndpoint, content);
            if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                return response;

            var retrySeconds = GetRetrySeconds(response);
            response.Dispose();
            log?.Invoke($"Rate limited (429), waiting {retrySeconds}s before retrying...");
            await Task.Delay(TimeSpan.FromSeconds(retrySeconds));
        }
    }

    private static int GetRetrySeconds(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta)
            return Math.Max(1, (int)Math.Ceiling(delta.TotalSeconds));

        if (retryAfter?.Date is { } date)
            return Math.Max(1, (int)Math.Ceiling((date - DateTimeOffset.UtcNow).TotalSeconds));

        return 30;
    }

    private async Task RetryQueuedAsync()
    {
        if (_retryQueue.Count == 0 || DateTime.UtcNow < _nextRetryAt)
            return;

        Log($"Retrying {_retryQueue.Count} queued upload(s)...");
        var count = _retryQueue.Count;
        for (var i = 0; i < count; i++)
        {
            var filePath = _retryQueue.Dequeue();
            try
            {
                using var response = await PostFileAsync(_httpClient, filePath, Log);

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<UploadResponse>();
                    var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/');
                    var reportUrl = $"{baseUrl}/report/{payload?.Id}";
                    _log($"Retry succeeded: {reportUrl}", reportUrl);
                    continue;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var errors = await response.Content.ReadFromJsonAsync<List<string>>();
                    Log($"Retry failed: {string.Join(", ", errors ?? ["Unauthorized"])}");
                    continue;
                }

                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log($"Retry failed, will try again: {ex.Message}");
                _retryQueue.Enqueue(filePath);
            }
        }

        if (_retryQueue.Count > 0)
            _nextRetryAt = DateTime.UtcNow.AddSeconds(30);
    }
}
