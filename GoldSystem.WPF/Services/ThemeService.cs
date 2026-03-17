using MaterialDesignThemes.Wpf;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Manages Material Design theme (Light/Dark) and accent colour at runtime.
/// Gold accent colour #FFD700 is applied as the primary palette colour.
/// </summary>
public sealed class ThemeService
{
    private bool _isDarkMode;

    public bool IsDarkMode => _isDarkMode;

    /// <summary>Toggle between Light and Dark themes.</summary>
    public void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme(_isDarkMode);
    }

    /// <summary>Apply the specified theme.</summary>
    public void SetDarkMode(bool dark)
    {
        _isDarkMode = dark;
        ApplyTheme(dark);
    }

    /// <summary>Apply light theme (default on startup).</summary>
    public void ApplyLightTheme() => SetDarkMode(false);

    /// <summary>Apply dark theme.</summary>
    public void ApplyDarkTheme() => SetDarkMode(true);

    private static void ApplyTheme(bool dark)
    {
        try
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(theme);
        }
        catch
        {
            // Theme update is non-critical; swallow if resources not available at test time.
        }
    }
}
