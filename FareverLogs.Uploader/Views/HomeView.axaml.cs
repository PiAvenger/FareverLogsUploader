using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FareverLogs.Uploader.ViewModels;

namespace FareverLogs.Uploader.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is HomeViewModel vm)
            vm.Refresh();
    }

    private async void OnBrowseFolderClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title       = "Select Farever Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0 && DataContext is HomeViewModel vm)
            vm.FareverFolder = folders[0].Path.LocalPath;
    }
}
