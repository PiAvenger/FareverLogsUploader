using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace FareverLogs.Uploader.Navigation;

public sealed partial class NavigationService : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly Stack<object>    _stack = new();

    [ObservableProperty] private object? _currentView;

    public NavigationService(IServiceProvider services) => _services = services;

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
