using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.IO;
using System.Text.Json;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Persists all settings groups to a single AppSettings.json file
/// located in the application base directory.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    // ── Root document shape ───────────────────────────────────────────────────
    private sealed class AppSettingsDocument
    {
        public CompanySettings  Company   { get; set; } = new();
        public TaxSettings      Tax       { get; set; } = new();
        public ThemeSettings    Theme     { get; set; } = new();
        public BackupSettings   Backup    { get; set; } = new();
        public UserPreferences  User      { get; set; } = new();
        public AdvancedSettings Advanced  { get; set; } = new();
    }

    public SettingsService(string? filePath = null)
    {
        _filePath = filePath
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.json");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AppSettingsDocument> LoadDocumentAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return new AppSettingsDocument();

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);
            return JsonSerializer.Deserialize<AppSettingsDocument>(json, _jsonOptions)
                   ?? new AppSettingsDocument();
        }
        catch
        {
            return new AppSettingsDocument();
        }
    }

    private async Task SaveDocumentAsync(AppSettingsDocument doc, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(doc, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    // ── Company ───────────────────────────────────────────────────────────────

    public async Task<CompanySettings> LoadCompanySettingsAsync(CancellationToken ct = default)
        => (await LoadDocumentAsync(ct)).Company;

    public async Task SaveCompanySettingsAsync(CompanySettings settings, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(ct);
        doc.Company = settings;
        await SaveDocumentAsync(doc, ct);
    }

    // ── Tax ───────────────────────────────────────────────────────────────────

    public async Task<TaxSettings> LoadTaxSettingsAsync(CancellationToken ct = default)
        => (await LoadDocumentAsync(ct)).Tax;

    public async Task SaveTaxSettingsAsync(TaxSettings settings, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(ct);
        doc.Tax = settings;
        await SaveDocumentAsync(doc, ct);
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

    public async Task<ThemeSettings> LoadThemeSettingsAsync(CancellationToken ct = default)
        => (await LoadDocumentAsync(ct)).Theme;

    public async Task SaveThemeSettingsAsync(ThemeSettings settings, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(ct);
        doc.Theme = settings;
        await SaveDocumentAsync(doc, ct);
    }

    // ── Backup ────────────────────────────────────────────────────────────────

    public async Task<BackupSettings> LoadBackupSettingsAsync(CancellationToken ct = default)
        => (await LoadDocumentAsync(ct)).Backup;

    public async Task SaveBackupSettingsAsync(BackupSettings settings, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(ct);
        doc.Backup = settings;
        await SaveDocumentAsync(doc, ct);
    }

    // ── User Preferences ─────────────────────────────────────────────────────

    public async Task<UserPreferences> LoadUserPreferencesAsync(CancellationToken ct = default)
        => (await LoadDocumentAsync(ct)).User;

    public async Task SaveUserPreferencesAsync(UserPreferences prefs, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(ct);
        doc.User = prefs;
        await SaveDocumentAsync(doc, ct);
    }

    // ── Advanced ──────────────────────────────────────────────────────────────

    public async Task<AdvancedSettings> LoadAdvancedSettingsAsync(CancellationToken ct = default)
        => (await LoadDocumentAsync(ct)).Advanced;

    public async Task SaveAdvancedSettingsAsync(AdvancedSettings settings, CancellationToken ct = default)
    {
        var doc = await LoadDocumentAsync(ct);
        doc.Advanced = settings;
        await SaveDocumentAsync(doc, ct);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    public async Task ResetToDefaultsAsync(CancellationToken ct = default)
        => await SaveDocumentAsync(new AppSettingsDocument(), ct);
}
