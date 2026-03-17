using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty] private bool _isDarkMode;
    private readonly ThemeService _themeService;

    public SettingsViewModel(NavigationService navigation, AppState appState, ThemeService themeService)
        : base(navigation, appState)
    {
        _themeService = themeService;
        _isDarkMode = themeService.IsDarkMode;
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        _themeService.SetDarkMode(value);
    }
}
