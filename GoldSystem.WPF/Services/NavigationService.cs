using System.Windows.Controls;

namespace GoldSystem.WPF.Services;

/// <summary>
/// MVVM navigation service – manages page/view transitions within the application shell.
/// Full implementation will be completed in Phase 2.
/// </summary>
public class NavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public bool CanNavigateBack => _frame?.CanGoBack ?? false;
    public bool CanNavigateForward => _frame?.CanGoForward ?? false;

    public void NavigateTo<TPage>(object? parameter = null) where TPage : Page, new()
    {
        if (_frame is null)
            throw new InvalidOperationException("NavigationService has not been initialized. Call Initialize(frame) first.");

        var page = new TPage();
        _frame.Navigate(page, parameter);
    }

    public void NavigateBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    public void NavigateForward()
    {
        if (_frame?.CanGoForward == true)
            _frame.GoForward();
    }
}
