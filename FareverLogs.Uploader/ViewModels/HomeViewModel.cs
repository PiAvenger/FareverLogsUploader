using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FareverLogs.Uploader.Config;
using FareverLogs.Uploader.Navigation;

namespace FareverLogs.Uploader.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly AppConfig         _config;
    private readonly NavigationService _nav;

    private const string DefaultSteamPath =
        @"C:\Program Files (x86)\Steam\steamapps\common\Farever\dps-meter\combat-logs";

    [ObservableProperty] private string _fareverFolder;
    [ObservableProperty] private string _folderError = "";

    public bool HasFolderError => !string.IsNullOrEmpty(_folderError);

    public HomeViewModel(AppConfig config, NavigationService nav)
    {
        _config = config;
        _nav    = nav;

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
        FolderError = "";
        _config.FareverFolder = value;
        _config.Save();
    }

    partial void OnFolderErrorChanged(string value) =>
        OnPropertyChanged(nameof(HasFolderError));

    public string Username => string.IsNullOrEmpty(_config.DiscordUsername)
        ? "Adventurer"
        : _config.DiscordUsername;

    public void Refresh() => OnPropertyChanged(nameof(Username));

    [RelayCommand]
    private void GoToLiveLog()
    {
        if (!Directory.Exists(FareverFolder))
        {
            FolderError = "Farever folder not found. Please set a valid path.";
            return;
        }
        FolderError = "";
        _nav.NavigateTo<LiveLogViewModel>();
    }

    [RelayCommand]
    private void GoToUploadExisting()
    {
        if (!Directory.Exists(FareverFolder))
        {
            FolderError = "Farever folder not found. Please set a valid path.";
            return;
        }
        FolderError = "";
        _nav.NavigateTo<UploadExistingViewModel>();
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
