using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.WPF.Services;
using System.Collections.ObjectModel;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// ViewModel for the Theme & Appearance settings tab.
/// Supports live preview (toggle) and persisting preferences.
/// </summary>
public sealed partial class ThemeSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ThemeService     _themeService;

    [ObservableProperty] private bool   _isDarkMode       = false;
    [ObservableProperty] private string _primaryColor     = "#FFD700";
    [ObservableProperty] private string _accentColor      = "#9C27B0";
    [ObservableProperty] private string _selectedFontSize = "Normal";
    [ObservableProperty] private string _selectedDateFormat  = "dd-MMM-yyyy";
    [ObservableProperty] private string _currencySymbol    = "₹";
    [ObservableProperty] private bool   _showSplashScreen  = true;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private string _statusMessage     = string.Empty;
    [ObservableProperty] private bool   _hasError;

    // ── Static option lists ───────────────────────────────────────────────────
    public ObservableCollection<string> FontSizes { get; } =
        new() { "Small", "Normal", "Large" };

    public ObservableCollection<string> DateFormats { get; } =
        new() { "dd-MMM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };

    public ObservableCollection<string> AvailableColors { get; } =
        new() { "#FFD700", "#F44336", "#2196F3", "#4CAF50", "#9C27B0", "#FF9800", "#00BCD4" };

    public ThemeSettingsViewModel(ISettingsService settingsService, ThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService    = themeService;
    }

    // ── Preview live toggle ───────────────────────────────────────────────────

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeService.SetDarkMode(value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        var s = await _settingsService.LoadThemeSettingsAsync();
        IsDarkMode          = s.IsDarkMode;
        PrimaryColor        = s.PrimaryColor;
        AccentColor         = s.AccentColor;
        SelectedFontSize    = s.FontSize;
        SelectedDateFormat  = s.DateFormat;
        CurrencySymbol      = s.CurrencySymbol;
        ShowSplashScreen    = s.ShowSplashScreen;
    }

    [RelayCommand]
    public void PreviewTheme()
    {
        _themeService.SetDarkMode(IsDarkMode);
    }

    [RelayCommand]
    public async Task ApplyThemeAsync()
    {
        IsSaving = true;
        HasError = false;
        try
        {
            _themeService.SetDarkMode(IsDarkMode);
            var s = new ThemeSettings
            {
                IsDarkMode       = IsDarkMode,
                PrimaryColor     = PrimaryColor,
                AccentColor      = AccentColor,
                FontSize         = SelectedFontSize,
                DateFormat       = SelectedDateFormat,
                CurrencySymbol   = CurrencySymbol,
                ShowSplashScreen = ShowSplashScreen
            };
            await _settingsService.SaveThemeSettingsAsync(s);
            StatusMessage = "Theme applied and saved.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error applying theme: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task ResetToDefaultAsync()
    {
        var d = new ThemeSettings();
        IsDarkMode         = d.IsDarkMode;
        PrimaryColor       = d.PrimaryColor;
        AccentColor        = d.AccentColor;
        SelectedFontSize   = d.FontSize;
        SelectedDateFormat = d.DateFormat;
        CurrencySymbol     = d.CurrencySymbol;
        ShowSplashScreen   = d.ShowSplashScreen;
        _themeService.SetDarkMode(false);
        StatusMessage = string.Empty;
        await Task.CompletedTask;
    }
}
