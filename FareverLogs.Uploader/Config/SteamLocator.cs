using Microsoft.Win32;

namespace FareverLogs.Uploader.Config;

internal static class SteamLocator
{
    private const string SubFolder = @"dps-meter\combat-logs";

    public static string? FindFareverLogDir()
    {
        var steamPath = GetSteamPath();
        if (steamPath is null) return null;

        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath)) return null;

        foreach (var libraryRoot in ParseLibraryPaths(steamPath, vdfPath))
        {
            var result = FindInLibrary(libraryRoot);
            if (result is not null) return result;
        }

        return null;
    }

    private const string AppId = "3672400";

    private static string? FindInLibrary(string libraryRoot)
    {
        var steamapps = Path.Combine(libraryRoot, "steamapps");
        var acfPath   = Path.Combine(steamapps, $"appmanifest_{AppId}.acf");
        if (!File.Exists(acfPath)) return null;

        var installDir = TryReadInstallDir(acfPath);
        if (installDir is null) return null;

        var logDir = Path.Combine(steamapps, "common", installDir, SubFolder);
        return Directory.Exists(logDir) ? logDir : null;
    }

    private static string? TryReadInstallDir(string acfPath)
    {
        try   { return ExtractValue(File.ReadAllText(acfPath), "installdir"); }
        catch { return null; }
    }

    private static IEnumerable<string> ParseLibraryPaths(string steamRoot, string vdfPath)
    {
        yield return steamRoot;

        string text;
        try { text = File.ReadAllText(vdfPath); }
        catch { yield break; }

        var pos = 0;
        while (true)
        {
            var idx = text.IndexOf("\"path\"", pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) break;

            idx += 6;
            while (idx < text.Length && text[idx] is '\t' or ' ') idx++;

            if (idx < text.Length && text[idx] == '"')
            {
                idx++;
                var end = text.IndexOf('"', idx);
                if (end > 0)
                {
                    var path = text[idx..end].Replace("\\\\", "\\");
                    if (!path.Equals(steamRoot, StringComparison.OrdinalIgnoreCase)
                        && Directory.Exists(path))
                        yield return path;
                    pos = end + 1;
                    continue;
                }
            }
            pos = idx + 1;
        }
    }

    private static string? ExtractValue(string text, string key)
    {
        var search = $"\"{key}\"";
        var idx = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        idx += search.Length;
        while (idx < text.Length && text[idx] is '\t' or ' ') idx++;
        if (idx >= text.Length || text[idx] != '"') return null;

        idx++;
        var end = text.IndexOf('"', idx);
        return end < 0 ? null : text[idx..end];
    }

    private static string? GetSteamPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            return key?.GetValue("InstallPath") as string;
        }
        catch { return null; }
    }
}
