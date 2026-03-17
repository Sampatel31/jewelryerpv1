using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

public sealed partial class CategoryViewModel : BaseViewModel
{
    [ObservableProperty] private bool _isLoading;

    public CategoryViewModel(NavigationService navigation, AppState appState)
        : base(navigation, appState) { }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        await Task.CompletedTask;
        IsLoading = false;
    }
}
