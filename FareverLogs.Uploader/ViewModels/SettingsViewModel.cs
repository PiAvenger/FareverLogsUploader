using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FareverLogs.Uploader.Config;
using FareverLogs.Uploader.Navigation;

namespace FareverLogs.Uploader.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppConfig         _config;
    private readonly NavigationService _nav;

    private const string DefaultSteamPath =
        @"C:\Program Files (x86)\Steam\steamapps\common\Farever\dps-meter\combat-logs";

    [ObservableProperty] private string _fareverFolder;
    [ObservableProperty] private string _folderError = "";

    [ObservableProperty] private bool _archiveOldLogs;
    [ObservableProperty] private int  _archiveOlderThanDays;
    [ObservableProperty] private bool _deleteOldLogs;
    [ObservableProperty] private int  _deleteOlderThanDays;

    public bool HasFolderError => !string.IsNullOrEmpty(_folderError);

#if DEBUG
    public bool IsServerVisible => true;
#else
    public bool IsServerVisible => false;
#endif

    public string ServerUrl => _config.ServerUrl;

    public string Username => string.IsNullOrEmpty(_config.DiscordUsername)
        ? "Adventurer"
        : _config.DiscordUsername;

    public SettingsViewModel(AppConfig config, NavigationService nav)
    {
        _config = config;
        _nav    = nav;

        _archiveOldLogs       = config.ArchiveOldLogs;
        _archiveOlderThanDays = config.ArchiveOlderThanDays;
        _deleteOldLogs        = config.DeleteOldLogs;
        _deleteOlderThanDays  = config.DeleteOlderThanDays;

        if (!string.IsNullOrEmpty(config.FareverFolder))
        {
            _fareverFolder = config.FareverFolder;
        }
        else
        {
            var detected = SteamLocator.FindFareverLogDir()
                ?? (Directory.Exists(DefaultSteamPath) ? DefaultSteamPath : "");
            _fareverFolder = detected;
            if (!string.IsNullOrEmpty(detected))
            {
                config.FareverFolder = detected;
                config.Save();
            }
        }
    }

    partial void OnFareverFolderChanged(string value)
    {
        _config.FareverFolder = value;
        _config.Save();
        FolderError = string.IsNullOrEmpty(value) || Directory.Exists(value)
            ? ""
            : "Folder not found.";
    }

    partial void OnFolderErrorChanged(string value) =>
        OnPropertyChanged(nameof(HasFolderError));

    partial void OnArchiveOldLogsChanged(bool value)
    {
        _config.ArchiveOldLogs = value;
        _config.Save();
    }

    partial void OnArchiveOlderThanDaysChanged(int value)
    {
        _config.ArchiveOlderThanDays = value;
        _config.Save();
    }

    partial void OnDeleteOldLogsChanged(bool value)
    {
        _config.DeleteOldLogs = value;
        _config.Save();
    }

    partial void OnDeleteOlderThanDaysChanged(int value)
    {
        _config.DeleteOlderThanDays = value;
        _config.Save();
    }

    [RelayCommand]
    private void Logout()
    {
        _config.JwtToken        = "";
        _config.DiscordUsername = "";
        _config.Save();
        _nav.NavigateToRoot<LoginViewModel>();
    }
}
