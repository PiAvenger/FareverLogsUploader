using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FareverLogs.Uploader.Updates;

public enum UpdateSeverity
{
    None,
    Build,
    Minor,
    Major
}

public readonly record struct UpdateNotice(UpdateSeverity Severity, string Message, string? ReleaseUrl);

public static class UpdateChecker
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/PiAvenger/FareverLogsUploader/releases/latest";

    public static async Task<UpdateNotice?> CheckAsync(HttpClient httpClient, Version currentVersion, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FareverLogs.Uploader", currentVersion.ToString(3)));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            if (!doc.RootElement.TryGetProperty("tag_name", out var tagProp)) return null;

            var tag = tagProp.GetString()?.TrimStart('v', 'V');
            if (string.IsNullOrEmpty(tag) || !Version.TryParse(tag, out var latest)) return null;

            var releaseUrl = doc.RootElement.TryGetProperty("html_url", out var urlProp)
                ? urlProp.GetString()
                : null;

            return Evaluate(currentVersion, latest, releaseUrl);
        }
        catch
        {
            return null;
        }
    }

    private static UpdateNotice? Evaluate(Version current, Version latest, string? releaseUrl)
    {
        if (latest.Major != current.Major)
            return latest.Major > current.Major
                ? new UpdateNotice(UpdateSeverity.Major, "This version of the Uploader is outdated.  Please get the latest release.", releaseUrl)
                : null;

        if (latest.Minor != current.Minor)
            return latest.Minor > current.Minor
                ? new UpdateNotice(UpdateSeverity.Minor, "New updates are available for the Uploader", releaseUrl)
                : null;

        if (latest.Build > current.Build)
            return new UpdateNotice(UpdateSeverity.Build, "A new uploader build is available", releaseUrl);

        return null;
    }
}
