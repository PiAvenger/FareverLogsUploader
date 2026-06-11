using CommunityToolkit.Mvvm.ComponentModel;
using FareverLogs.Uploader.Config;

namespace FareverLogs.Uploader.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly AppConfig _config;

    public LiveLogViewModel        LiveLog        { get; }
    public UploadExistingViewModel UploadExisting { get; }
    public SettingsViewModel       Settings       { get; }

    public HomeViewModel(
        AppConfig config,
        LiveLogViewModel liveLog,
        UploadExistingViewModel uploadExisting,
        SettingsViewModel settings)
    {
        _config        = config;
        LiveLog        = liveLog;
        UploadExisting = uploadExisting;
        Settings       = settings;
    }

    public string Username => string.IsNullOrEmpty(_config.DiscordUsername)
        ? "Adventurer"
        : _config.DiscordUsername;

    public void Refresh() => OnPropertyChanged(nameof(Username));
}
