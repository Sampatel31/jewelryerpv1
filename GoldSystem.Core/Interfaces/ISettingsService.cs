using GoldSystem.Core.Models;

namespace GoldSystem.Core.Interfaces;

/// <summary>
/// Persists and retrieves all application settings groups.
/// Settings are stored in AppSettings.json and optionally the database.
/// </summary>
public interface ISettingsService
{
    // ── Company ───────────────────────────────────────────────────────────────
    Task<CompanySettings>  LoadCompanySettingsAsync(CancellationToken ct = default);
    Task                   SaveCompanySettingsAsync(CompanySettings settings, CancellationToken ct = default);

    // ── Tax ───────────────────────────────────────────────────────────────────
    Task<TaxSettings>      LoadTaxSettingsAsync(CancellationToken ct = default);
    Task                   SaveTaxSettingsAsync(TaxSettings settings, CancellationToken ct = default);

    // ── Theme ─────────────────────────────────────────────────────────────────
    Task<ThemeSettings>    LoadThemeSettingsAsync(CancellationToken ct = default);
    Task                   SaveThemeSettingsAsync(ThemeSettings settings, CancellationToken ct = default);

    // ── Backup ────────────────────────────────────────────────────────────────
    Task<BackupSettings>   LoadBackupSettingsAsync(CancellationToken ct = default);
    Task                   SaveBackupSettingsAsync(BackupSettings settings, CancellationToken ct = default);

    // ── User Preferences ─────────────────────────────────────────────────────
    Task<UserPreferences>  LoadUserPreferencesAsync(CancellationToken ct = default);
    Task                   SaveUserPreferencesAsync(UserPreferences prefs, CancellationToken ct = default);

    // ── Advanced ──────────────────────────────────────────────────────────────
    Task<AdvancedSettings> LoadAdvancedSettingsAsync(CancellationToken ct = default);
    Task                   SaveAdvancedSettingsAsync(AdvancedSettings settings, CancellationToken ct = default);

    // ── Utilities ─────────────────────────────────────────────────────────────
    /// <summary>Reset all settings groups to their factory defaults.</summary>
    Task ResetToDefaultsAsync(CancellationToken ct = default);
}
