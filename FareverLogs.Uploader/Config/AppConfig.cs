using System.Text.Json;
using System.Text.Json.Serialization;

namespace FareverLogs.Uploader.Config;

/// <summary>
/// Application settings
/// Persisted as JSON in %APPDATA%\FareverLogs\.
/// </summary>
public sealed class AppConfig
{
       // -- game folder
    public string FareverFolder { get; set; } = "";

    // -- auth
    public string ServerUrl { get; set; } = "https://fareverlogs.com";
    public string JwtToken        { get; set; } = "";
    public string DiscordUsername { get; set; } = "";

    // -- log maintenance (run on startup)
    public bool ArchiveOldLogs       { get; set; } = false;
    public int  ArchiveOlderThanDays { get; set; } = 7;
    public bool DeleteOldLogs        { get; set; } = false;
    public int  DeleteOlderThanDays  { get; set; } = 30;

    // -- persistence

    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FareverLogs");
    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented            = true,
        PropertyNamingPolicy     = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition   = JsonIgnoreCondition.WhenWritingNull,
    };

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, JsonOpts) ?? new AppConfig();
            }
        }
        catch { /* corrupt config — use defaults */ }
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOpts));
        }
        catch { /* ignore save failures */ }
    }
}
