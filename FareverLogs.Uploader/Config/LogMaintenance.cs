namespace FareverLogs.Uploader.Config;

/// <summary>
/// Startup housekeeping for the combat-log folder: optionally archives
/// older logs into an "archive" subfolder and deletes the oldest ones.
/// </summary>
public static class LogMaintenance
{
    private const string ArchiveDirName = "archive";

    private static readonly string[] LogPatterns = ["*.json", "*.json.gz"];

    public static void Run(AppConfig config)
    {
        var dir = config.FareverFolder;
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return;

        var archiveDir = Path.Combine(dir, ArchiveDirName);

        // Delete first so the threshold also clears stale files already in the
        // archive; otherwise archived logs would accumulate forever.
        if (config.DeleteOldLogs)
        {
            var cutoff = DateTime.Now.AddDays(-Math.Max(0, config.DeleteOlderThanDays));
            DeleteOlderThan(dir, cutoff);
            DeleteOlderThan(archiveDir, cutoff);
        }

        if (config.ArchiveOldLogs)
        {
            var cutoff = DateTime.Now.AddDays(-Math.Max(0, config.ArchiveOlderThanDays));
            ArchiveOlderThan(dir, archiveDir, cutoff);
        }
    }

    private static void DeleteOlderThan(string dir, DateTime cutoff)
    {
        if (!Directory.Exists(dir))
            return;

        foreach (var file in EnumerateLogs(dir))
        {
            try
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
            catch { /* skip files we can't touch (locked, perms, etc.) */ }
        }
    }

    private static void ArchiveOlderThan(string dir, string archiveDir, DateTime cutoff)
    {
        foreach (var file in EnumerateLogs(dir))
        {
            try
            {
                if (File.GetLastWriteTime(file) >= cutoff)
                    continue;

                Directory.CreateDirectory(archiveDir);
                var dest = Path.Combine(archiveDir, Path.GetFileName(file));
                if (File.Exists(dest))
                    File.Delete(dest);
                File.Move(file, dest);
            }
            catch { /* skip files we can't touch (locked, perms, etc.) */ }
        }
    }

    private static IEnumerable<string> EnumerateLogs(string dir) =>
        LogPatterns.SelectMany(p => Directory.EnumerateFiles(dir, p)).ToList();
}
