using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.Views;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Base class for all ViewModels that receive navigation and state services.
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    protected readonly NavigationService Navigation;
    protected readonly AppState AppState;

    protected BaseViewModel(NavigationService navigation, AppState appState)
    {
        Navigation = navigation;
        AppState = appState;
    }

    /// <summary>Called when this view becomes active (optional override).</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    /// <summary>Called when navigating away from this view (optional override).</summary>
    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
}
