using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.WPF.Services;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Main Settings ViewModel – coordinates all 6 settings tabs and delegates
/// to the individual tab ViewModels.
/// </summary>
public sealed partial class SettingsViewModel : BaseViewModel
{
    // ── Tab ViewModels ────────────────────────────────────────────────────────
    public CompanySettingsViewModel  Company  { get; }
    public TaxSettingsViewModel      Tax      { get; }
    public ThemeSettingsViewModel    Theme    { get; }
    public BackupSettingsViewModel   Backup   { get; }
    public UserPreferencesViewModel  Prefs    { get; }
    public AdvancedSettingsViewModel Advanced { get; }

    // ── Shell state ───────────────────────────────────────────────────────────
    [ObservableProperty] private int    _selectedTabIndex = 0;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel(
        NavigationService        navigation,
        AppState                 appState,
        CompanySettingsViewModel  company,
        TaxSettingsViewModel      tax,
        ThemeSettingsViewModel    theme,
        BackupSettingsViewModel   backup,
        UserPreferencesViewModel  prefs,
        AdvancedSettingsViewModel advanced)
        : base(navigation, appState)
    {
        Company  = company;
        Tax      = tax;
        Theme    = theme;
        Backup   = backup;
        Prefs    = prefs;
        Advanced = advanced;
    }

    // ── Navigation lifecycle ──────────────────────────────────────────────────

    public override async Task OnNavigatedToAsync()
    {
        await Task.WhenAll(
            Company.LoadCommand.ExecuteAsync(null),
            Tax.LoadCommand.ExecuteAsync(null),
            Theme.LoadCommand.ExecuteAsync(null),
            Backup.LoadCommand.ExecuteAsync(null),
            Prefs.LoadCommand.ExecuteAsync(null),
            Advanced.LoadCommand.ExecuteAsync(null));
    }

    // ── Aggregate save ────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task SaveAllAsync()
    {
        IsSaving = true;
        try
        {
            await Task.WhenAll(
                Company.SaveCommand.ExecuteAsync(null),
                Tax.SaveCommand.ExecuteAsync(null),
                Theme.ApplyThemeCommand.ExecuteAsync(null),
                Backup.SaveSettingsCommand.ExecuteAsync(null),
                Prefs.SaveCommand.ExecuteAsync(null),
                Advanced.SaveCommand.ExecuteAsync(null));
            StatusMessage = "All settings saved successfully.";
        }
        finally
        {
            IsSaving = false;
        }
    }
}
