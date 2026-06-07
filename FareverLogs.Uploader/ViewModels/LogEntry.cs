namespace FareverLogs.Uploader.ViewModels;

public record LogEntry(string Text, string? Url = null)
{
    public bool HasUrl => Url is not null;
}
