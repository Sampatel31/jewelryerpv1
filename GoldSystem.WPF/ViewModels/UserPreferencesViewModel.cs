using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.ObjectModel;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for the User Preferences settings tab.
/// </summary>
public sealed partial class UserPreferencesViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private bool   _showTipsOnStartup      = true;
    [ObservableProperty] private string _defaultBranch          = string.Empty;
    [ObservableProperty] private int    _autoLogoutMinutes       = 30;
    [ObservableProperty] private bool   _confirmOnDelete         = true;
    [ObservableProperty] private bool   _showNotifications       = true;
    [ObservableProperty] private bool   _soundEnabled            = true;
    [ObservableProperty] private bool   _autoPrintBillAfterSave  = true;
    [ObservableProperty] private int    _decimalPlaces           = 2;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private string _statusMessage           = string.Empty;
    [ObservableProperty] private bool   _hasError;

    public ObservableCollection<int>    DecimalPlacesOptions { get; } = new() { 0, 1, 2, 3 };
    public ObservableCollection<int>    AutoLogoutOptions    { get; } = new() { 5, 10, 15, 30, 60, 120 };

    public UserPreferencesViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        var p = await _settingsService.LoadUserPreferencesAsync();
        ShowTipsOnStartup     = p.ShowTipsOnStartup;
        DefaultBranch         = p.DefaultBranch;
        AutoLogoutMinutes     = p.AutoLogoutMinutes;
        ConfirmOnDelete       = p.ConfirmOnDelete;
        ShowNotifications     = p.ShowNotifications;
        SoundEnabled          = p.SoundEnabled;
        AutoPrintBillAfterSave = p.AutoPrintBillAfterSave;
        DecimalPlaces         = p.DecimalPlaces;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        IsSaving = true;
        HasError = false;
        try
        {
            var p = new UserPreferences
            {
                ShowTipsOnStartup      = ShowTipsOnStartup,
                DefaultBranch          = DefaultBranch,
                AutoLogoutMinutes      = AutoLogoutMinutes,
                ConfirmOnDelete        = ConfirmOnDelete,
                ShowNotifications      = ShowNotifications,
                SoundEnabled           = SoundEnabled,
                AutoPrintBillAfterSave = AutoPrintBillAfterSave,
                DecimalPlaces          = DecimalPlaces
            };
            await _settingsService.SaveUserPreferencesAsync(p);
            StatusMessage = "Preferences saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task ResetAsync()
    {
        var d = new UserPreferences();
        ShowTipsOnStartup      = d.ShowTipsOnStartup;
        DefaultBranch          = d.DefaultBranch;
        AutoLogoutMinutes      = d.AutoLogoutMinutes;
        ConfirmOnDelete        = d.ConfirmOnDelete;
        ShowNotifications      = d.ShowNotifications;
        SoundEnabled           = d.SoundEnabled;
        AutoPrintBillAfterSave = d.AutoPrintBillAfterSave;
        DecimalPlaces          = d.DecimalPlaces;
        StatusMessage = string.Empty;
        await Task.CompletedTask;
    }
}
