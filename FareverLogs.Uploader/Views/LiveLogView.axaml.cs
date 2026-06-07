using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FareverLogs.Uploader.ViewModels;

namespace FareverLogs.Uploader.Views;

public partial class LiveLogView : UserControl
{
    private LiveLogViewModel? _vm;

    public LiveLogView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
            _vm.LogMessages.CollectionChanged -= OnLogChanged;

        _vm = DataContext as LiveLogViewModel;

        if (_vm is not null)
            _vm.LogMessages.CollectionChanged += OnLogChanged;
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_vm is null || _vm.LogMessages.Count == 0) return;
        Dispatcher.UIThread.Post(() =>
            LogBox.ScrollIntoView(_vm.LogMessages[^1]));
    }

    private void OnUrlClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
