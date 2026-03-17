using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

public sealed partial class SyncStatusViewModel : BaseViewModel
{
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private int _failedCount;
    [ObservableProperty] private DateTime _lastSyncTime;
    [ObservableProperty] private bool _isLoading;

    public SyncStatusViewModel(NavigationService navigation, AppState appState)
        : base(navigation, appState) { }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        await Task.CompletedTask;
        IsLoading = false;
    }
}
