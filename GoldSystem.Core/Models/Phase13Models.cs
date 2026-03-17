namespace GoldSystem.Core.Models;

/// <summary>Company profile and contact information stored in AppSettings.json.</summary>
public sealed class CompanySettings
{
    public string CompanyName { get; set; } = "Gold Jewellery Store";
    public string GSTIN       { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string Phone       { get; set; } = string.Empty;
    public string Address     { get; set; } = string.Empty;
    public string State       { get; set; } = string.Empty;
    public string City        { get; set; } = string.Empty;
    public string PostalCode  { get; set; } = string.Empty;
    public string BankName    { get; set; } = string.Empty;
    public string AccountNo   { get; set; } = string.Empty;
    public string IFSC        { get; set; } = string.Empty;
    public string LogoPath    { get; set; } = string.Empty;
}

/// <summary>Tax rate configuration (CGST/SGST/IGST) and charge defaults.</summary>
public sealed class TaxSettings
{
    public decimal CGSTRate               { get; set; } = 9m;
    public decimal SGSTRate               { get; set; } = 9m;
    public decimal IGSTRate               { get; set; } = 5m;
    public decimal ExemptThreshold        { get; set; } = 0m;
    public decimal DefaultMakingPercent   { get; set; } = 12m;
    public decimal DefaultWastagePercent  { get; set; } = 2m;
    public decimal DefaultStoneCharge     { get; set; } = 0m;
    public string  HSNCode                { get; set; } = "7108";
    public bool    ApplyToAllBills        { get; set; } = true;
}

/// <summary>UI theme, colour, font, and locale preferences.</summary>
public sealed class ThemeSettings
{
    public bool   IsDarkMode        { get; set; } = false;
    public string PrimaryColor      { get; set; } = "#FFD700";
    public string AccentColor       { get; set; } = "#9C27B0";
    public string FontSize          { get; set; } = "Normal";
    public string DateFormat        { get; set; } = "dd-MMM-yyyy";
    public string CurrencySymbol    { get; set; } = "₹";
    public bool   ShowSplashScreen  { get; set; } = true;
}

/// <summary>Backup location, schedule, and last-run timestamp.</summary>
public sealed class BackupSettings
{
    public string   BackupLocation      { get; set; } = string.Empty;
    public bool     AutoBackupEnabled   { get; set; } = true;
    public int      BackupIntervalHours { get; set; } = 24;
    public DateTime LastBackupTime      { get; set; } = DateTime.MinValue;
    public int      MaxBackupsToKeep    { get; set; } = 5;
}

/// <summary>Per-user application behaviour preferences.</summary>
public sealed class UserPreferences
{
    public bool   ShowTipsOnStartup      { get; set; } = true;
    public string DefaultBranch          { get; set; } = string.Empty;
    public int    AutoLogoutMinutes      { get; set; } = 30;
    public bool   ConfirmOnDelete        { get; set; } = true;
    public bool   ShowNotifications      { get; set; } = true;
    public bool   SoundEnabled           { get; set; } = true;
    public bool   AutoPrintBillAfterSave { get; set; } = true;
    public int    DecimalPlaces          { get; set; } = 2;
}

/// <summary>Advanced technical settings (DB, logging, cache, debug).</summary>
public sealed class AdvancedSettings
{
    public string DatabaseType         { get; set; } = "SQLite";
    public int    SyncIntervalMinutes  { get; set; } = 15;
    public string LogLevel             { get; set; } = "Info";
    public bool   DebugModeEnabled     { get; set; } = false;
    public long   CacheSizeBytes       { get; set; } = 0;
}

/// <summary>Metadata for a single database backup file.</summary>
public sealed record BackupMetadata(
    string   FilePath,
    DateTime CreatedAt,
    long     SizeBytes,
    string   Description)
{
    public string DisplayName => System.IO.Path.GetFileName(FilePath);
    public string SizeDisplay =>
        SizeBytes >= 1_048_576
            ? $"{SizeBytes / 1_048_576.0:F1} MB"
            : $"{SizeBytes / 1_024.0:F1} KB";
}

/// <summary>System runtime information displayed in the Advanced tab.</summary>
public sealed class SystemInfo
{
    public string AppVersion    { get; set; } = "1.0.0";
    public string DotNetVersion { get; set; } = System.Environment.Version.ToString();
    public long   DbSizeBytes   { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string DbSizeDisplay =>
        DbSizeBytes >= 1_048_576
            ? $"{DbSizeBytes / 1_048_576.0:F1} MB"
            : $"{DbSizeBytes / 1_024.0:F1} KB";
}
