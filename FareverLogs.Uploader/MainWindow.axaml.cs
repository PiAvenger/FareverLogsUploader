using Avalonia.Controls;
using FareverLogs.Uploader.Navigation;

namespace FareverLogs.Uploader;

public partial class MainWindow : Window
{
    public MainWindow(NavigationService nav)
    {
        InitializeComponent();
        DataContext = nav;
    }
}
