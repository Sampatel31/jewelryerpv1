using System.Windows.Controls;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Abstraction over the MVVM navigation service to enable unit-testing of ViewModels
/// without requiring a live WPF application.
/// </summary>
public interface INavigationService
{
    bool CanNavigateBack { get; }
    Type? CurrentViewType { get; }

    void Initialize(ContentControl contentHost);
    void NavigateTo<TView>(object? parameter = null) where TView : UserControl;
    void NavigateTo(Type viewType, object? parameter = null);
    bool NavigateBack();
    void NavigateToRoot<TView>(object? parameter = null) where TView : UserControl;
}
