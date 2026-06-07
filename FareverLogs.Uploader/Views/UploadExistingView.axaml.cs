using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FareverLogs.Uploader.ViewModels;

namespace FareverLogs.Uploader.Views;

public partial class UploadExistingView : UserControl
{
    private UploadExistingViewModel? _vm;

    public UploadExistingView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm is not null)
            _vm.Results.CollectionChanged -= OnResultsChanged;

        _vm = DataContext as UploadExistingViewModel;

        if (_vm is not null)
        {
            _vm.Results.CollectionChanged += OnResultsChanged;
            _vm.ScanFiles();
        }
    }

    private void OnResultsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_vm is null || _vm.Results.Count == 0) return;
        Dispatcher.UIThread.Post(() =>
            ResultsBox.ScrollIntoView(_vm.Results[^1]));
    }

    private void OnRefreshClicked(object? sender, RoutedEventArgs e) => _vm?.ScanFiles();

    private void OnUrlClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
