using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace GoldSystem.WPF.Services;

/// <summary>
/// MVVM navigation service – resolves view/viewmodel pairs from DI and
/// injects them into the shell's content presenter.
/// </summary>
public sealed class NavigationService : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<(Type ViewType, object? Parameter)> _backStack = new();
    private ContentControl? _contentHost;
    private Type? _currentViewType;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool CanNavigateBack => _backStack.Count > 0;

    public Type? CurrentViewType => _currentViewType;

    /// <summary>Binds the navigation service to the shell's main content area.</summary>
    public void Initialize(ContentControl contentHost)
    {
        _contentHost = contentHost;
    }

    /// <summary>Navigates to the view registered for <typeparamref name="TView"/>.</summary>
    public void NavigateTo<TView>(object? parameter = null) where TView : UserControl
        => NavigateTo(typeof(TView), parameter);

    /// <summary>Navigates to the view specified by type, pushing current view onto back-stack.</summary>
    public void NavigateTo(Type viewType, object? parameter = null)
    {
        if (_contentHost is null)
            throw new InvalidOperationException("NavigationService not initialised. Call Initialize(contentHost) first.");

        if (_currentViewType is not null)
            _backStack.Push((_currentViewType, parameter));

        ShowView(viewType, parameter);
    }

    /// <summary>Navigates to the previous view on the back-stack.</summary>
    public bool NavigateBack()
    {
        if (!CanNavigateBack) return false;
        var (viewType, parameter) = _backStack.Pop();
        ShowView(viewType, parameter);
        return true;
    }

    /// <summary>Clears the navigation history and navigates to the given view.</summary>
    public void NavigateToRoot<TView>(object? parameter = null) where TView : UserControl
    {
        _backStack.Clear();
        ShowView(typeof(TView), parameter);
    }

    private void ShowView(Type viewType, object? parameter)
    {
        var view = (UserControl)_serviceProvider.GetRequiredService(viewType);
        _currentViewType = viewType;
        _contentHost!.Content = view;
        OnPropertyChanged(nameof(CanNavigateBack));
        OnPropertyChanged(nameof(CurrentViewType));
    }
}
