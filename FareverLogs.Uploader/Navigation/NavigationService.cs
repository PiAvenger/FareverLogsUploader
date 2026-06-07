using CommunityToolkit.Mvvm.ComponentModel;
using FareverLogs.Uploader.Updates;
using Microsoft.Extensions.DependencyInjection;

namespace FareverLogs.Uploader.Navigation;

public sealed partial class NavigationService : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly Stack<object>    _stack = new();

    [ObservableProperty] private object? _currentView;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateBannerVisible))]
    [NotifyPropertyChangedFor(nameof(IsBuildUpdate))]
    [NotifyPropertyChangedFor(nameof(IsMinorUpdate))]
    [NotifyPropertyChangedFor(nameof(IsMajorUpdate))]
    private UpdateSeverity _updateSeverity;

    [ObservableProperty] private string _updateBannerMessage = "";

    public bool IsUpdateBannerVisible => UpdateSeverity != UpdateSeverity.None;
    public bool IsBuildUpdate         => UpdateSeverity == UpdateSeverity.Build;
    public bool IsMinorUpdate         => UpdateSeverity == UpdateSeverity.Minor;
    public bool IsMajorUpdate         => UpdateSeverity == UpdateSeverity.Major;

    public NavigationService(IServiceProvider services) => _services = services;

    public void ShowUpdateNotice(UpdateNotice notice)
    {
        UpdateBannerMessage = notice.Message;
        UpdateSeverity      = notice.Severity;
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var vm = _services.GetRequiredService<TViewModel>();
        _stack.Push(vm);
        CurrentView = vm;
    }

    public void NavigateToRoot<TViewModel>() where TViewModel : class
    {
        _stack.Clear();
        NavigateTo<TViewModel>();
    }

    public void GoBack()
    {
        if (_stack.Count <= 1) return;
        _stack.Pop();
        CurrentView = _stack.Peek();
    }
}
