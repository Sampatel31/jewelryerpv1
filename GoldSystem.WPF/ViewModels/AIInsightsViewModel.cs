using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

public sealed partial class AIInsightsViewModel : BaseViewModel
{
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _forecastSummary = string.Empty;
    [ObservableProperty] private string _trendSignal = string.Empty;

    public AIInsightsViewModel(NavigationService navigation, AppState appState)
        : base(navigation, appState) { }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        await Task.CompletedTask;
        IsLoading = false;
    }
}
