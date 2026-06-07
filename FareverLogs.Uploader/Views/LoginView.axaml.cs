using Avalonia.Controls;
using FareverLogs.Uploader.ViewModels;

namespace FareverLogs.Uploader.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is LoginViewModel vm && vm.IsAlreadyLoggedIn)
            vm.LoginCommand.Execute(null); // fast-path: skip login screen
    }
}
