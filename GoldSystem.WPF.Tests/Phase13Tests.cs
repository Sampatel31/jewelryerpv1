using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using Moq;
using System.IO;

namespace GoldSystem.WPF.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// Phase 13 – Settings & Customization Tests
// ═══════════════════════════════════════════════════════════════════════════════

// ── CompanySettings Model ─────────────────────────────────────────────────────

public class Phase13CompanySettingsModelTests
{
    [Fact]
    public void CompanySettings_DefaultValues_AreCorrect()
    {
        var s = new CompanySettings();
        Assert.Equal("Gold Jewellery Store", s.CompanyName);
        Assert.Equal(string.Empty, s.GSTIN);
        Assert.Equal(string.Empty, s.Email);
    }

    [Fact]
    public void TaxSettings_DefaultRates_AreCorrect()
    {
        var t = new TaxSettings();
        Assert.Equal(9m, t.CGSTRate);
        Assert.Equal(9m, t.SGSTRate);
        Assert.Equal(5m, t.IGSTRate);
        Assert.Equal("7108", t.HSNCode);
        Assert.True(t.ApplyToAllBills);
    }

    [Fact]
    public void ThemeSettings_DefaultValues_AreCorrect()
    {
        var th = new ThemeSettings();
        Assert.False(th.IsDarkMode);
        Assert.Equal("#FFD700", th.PrimaryColor);
        Assert.Equal("Normal", th.FontSize);
        Assert.Equal("dd-MMM-yyyy", th.DateFormat);
        Assert.True(th.ShowSplashScreen);
    }

    [Fact]
    public void BackupMetadata_DisplayName_ExtractsFileName()
    {
        var m = new BackupMetadata(@"C:\Backups\Backup_20260317_0230.zip",
            DateTime.Now, 1_048_576, "Test backup");
        Assert.Equal("Backup_20260317_0230.zip", m.DisplayName);
    }

    [Fact]
    public void BackupMetadata_SizeDisplay_FormatsCorrectly_MB()
    {
        var m = new BackupMetadata("test.zip", DateTime.Now, 2_097_152, "");
        Assert.Contains("MB", m.SizeDisplay);
        Assert.Contains("2", m.SizeDisplay);
    }

    [Fact]
    public void BackupMetadata_SizeDisplay_FormatsCorrectly_KB()
    {
        var m = new BackupMetadata("test.zip", DateTime.Now, 512 * 1024, "");
        Assert.Contains("KB", m.SizeDisplay);
    }

    [Fact]
    public void SystemInfo_DefaultDotNetVersion_IsNotEmpty()
    {
        var info = new SystemInfo();
        Assert.False(string.IsNullOrEmpty(info.DotNetVersion));
        Assert.Equal("1.0.0", info.AppVersion);
    }

    [Fact]
    public void UserPreferences_DefaultValues_AreCorrect()
    {
        var p = new UserPreferences();
        Assert.True(p.ShowTipsOnStartup);
        Assert.Equal(30, p.AutoLogoutMinutes);
        Assert.True(p.ConfirmOnDelete);
        Assert.Equal(2, p.DecimalPlaces);
    }

    [Fact]
    public void AdvancedSettings_DefaultValues_AreCorrect()
    {
        var a = new AdvancedSettings();
        Assert.Equal("SQLite", a.DatabaseType);
        Assert.Equal(15, a.SyncIntervalMinutes);
        Assert.Equal("Info", a.LogLevel);
        Assert.False(a.DebugModeEnabled);
    }
}

// ── Validation Tests ──────────────────────────────────────────────────────────

public class Phase13ValidationTests
{
    private static CompanySettingsViewModel CreateCompanyVm()
    {
        var svc = new Mock<ISettingsService>();
        svc.Setup(s => s.LoadCompanySettingsAsync(default)).ReturnsAsync(new CompanySettings());
        svc.Setup(s => s.SaveCompanySettingsAsync(It.IsAny<CompanySettings>(), default))
           .Returns(Task.CompletedTask);
        return new CompanySettingsViewModel(svc.Object);
    }

    [Theory]
    [InlineData("27AAPFU0939F1ZV", true)]
    [InlineData("29AABCT1234H1Z0", true)]
    [InlineData("", true)]            // empty is allowed
    [InlineData("INVALID", false)]
    [InlineData("12345678901234567890", false)]
    public void CompanyVm_GstinValidation_WorksCorrectly(string gstin, bool expected)
    {
        var vm = CreateCompanyVm();
        Assert.Equal(expected, vm.IsGstinValid(gstin));
    }

    [Theory]
    [InlineData("admin@company.com", true)]
    [InlineData("user@domain.in", true)]
    [InlineData("", true)]            // empty is allowed
    [InlineData("not-an-email", false)]
    [InlineData("missing@", false)]
    public void CompanyVm_EmailValidation_WorksCorrectly(string email, bool expected)
    {
        var vm = CreateCompanyVm();
        Assert.Equal(expected, vm.IsEmailValid(email));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(9, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void TaxVm_RateValidation_WorksCorrectly(decimal rate, bool expected)
    {
        Assert.Equal(expected, TaxSettingsViewModel.IsRateValid(rate));
    }
}

// ── SettingsService Tests (uses temp file) ────────────────────────────────────

public class Phase13SettingsServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly SettingsService _service;

    public Phase13SettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid():N}.json");
        _service  = new SettingsService(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
    }

    [Fact]
    public async Task SettingsService_SaveAndLoad_CompanySettings_RoundTrips()
    {
        var original = new CompanySettings
        {
            CompanyName = "Test Jewellers",
            GSTIN       = "27AAPFU0939F1ZV",
            Email       = "test@example.com",
            Phone       = "9876543210"
        };

        await _service.SaveCompanySettingsAsync(original);
        var loaded = await _service.LoadCompanySettingsAsync();

        Assert.Equal("Test Jewellers", loaded.CompanyName);
        Assert.Equal("27AAPFU0939F1ZV", loaded.GSTIN);
        Assert.Equal("test@example.com", loaded.Email);
    }

    [Fact]
    public async Task SettingsService_SaveAndLoad_TaxSettings_RoundTrips()
    {
        var tax = new TaxSettings { CGSTRate = 14m, SGSTRate = 14m, IGSTRate = 12m };
        await _service.SaveTaxSettingsAsync(tax);
        var loaded = await _service.LoadTaxSettingsAsync();

        Assert.Equal(14m, loaded.CGSTRate);
        Assert.Equal(14m, loaded.SGSTRate);
        Assert.Equal(12m, loaded.IGSTRate);
    }

    [Fact]
    public async Task SettingsService_SaveAndLoad_ThemeSettings_RoundTrips()
    {
        var theme = new ThemeSettings { IsDarkMode = true, PrimaryColor = "#FF0000", FontSize = "Large" };
        await _service.SaveThemeSettingsAsync(theme);
        var loaded = await _service.LoadThemeSettingsAsync();

        Assert.True(loaded.IsDarkMode);
        Assert.Equal("#FF0000", loaded.PrimaryColor);
        Assert.Equal("Large", loaded.FontSize);
    }

    [Fact]
    public async Task SettingsService_ResetToDefaults_RestoresAllDefaults()
    {
        var company = new CompanySettings { CompanyName = "Custom Name" };
        await _service.SaveCompanySettingsAsync(company);
        await _service.ResetToDefaultsAsync();
        var loaded = await _service.LoadCompanySettingsAsync();

        Assert.Equal("Gold Jewellery Store", loaded.CompanyName);
    }

    [Fact]
    public async Task SettingsService_LoadWithNoFile_ReturnsDefaults()
    {
        var loaded = await _service.LoadCompanySettingsAsync();
        Assert.Equal("Gold Jewellery Store", loaded.CompanyName);
    }

    [Fact]
    public async Task SettingsService_SaveAndLoad_UserPreferences_RoundTrips()
    {
        var prefs = new UserPreferences { AutoLogoutMinutes = 60, DecimalPlaces = 3, SoundEnabled = false };
        await _service.SaveUserPreferencesAsync(prefs);
        var loaded = await _service.LoadUserPreferencesAsync();

        Assert.Equal(60, loaded.AutoLogoutMinutes);
        Assert.Equal(3, loaded.DecimalPlaces);
        Assert.False(loaded.SoundEnabled);
    }
}

// ── BackupService Tests (uses temp folder) ────────────────────────────────────

public class Phase13BackupServiceTests : IDisposable
{
    private readonly string _tempDb;
    private readonly string _backupFolder;
    private readonly BackupService _service;

    public Phase13BackupServiceTests()
    {
        _tempDb       = Path.Combine(Path.GetTempPath(), $"test_gs_{Guid.NewGuid():N}.db");
        _backupFolder = Path.Combine(Path.GetTempPath(), $"backups_{Guid.NewGuid():N}");
        File.WriteAllText(_tempDb, "SQLite test content for backup testing");
        _service = new BackupService(_tempDb);
    }

    public void Dispose()
    {
        if (File.Exists(_tempDb))        File.Delete(_tempDb);
        if (Directory.Exists(_backupFolder)) Directory.Delete(_backupFolder, recursive: true);
    }

    [Fact]
    public async Task BackupService_BackupNow_CreatesZipFile()
    {
        var zipPath = await _service.BackupDatabaseAsync(_backupFolder, "unit test backup");

        Assert.True(File.Exists(zipPath));
        Assert.EndsWith(".zip", zipPath);
    }

    [Fact]
    public async Task BackupService_GetRecentBackups_ReturnsMetadata()
    {
        await _service.BackupDatabaseAsync(_backupFolder, "backup 1");
        await Task.Delay(50);
        await _service.BackupDatabaseAsync(_backupFolder, "backup 2");

        var list = await _service.GetRecentBackupsAsync(_backupFolder, 5);
        Assert.Equal(2, list.Count);
        Assert.All(list, m => Assert.EndsWith(".zip", m.FilePath));
    }

    [Fact]
    public async Task BackupService_DeleteOldBackups_KeepsOnlyMaxCount()
    {
        for (var i = 0; i < 4; i++)
        {
            await _service.BackupDatabaseAsync(_backupFolder, $"backup {i}");
            await Task.Delay(30);
        }

        await _service.DeleteOldBackupsAsync(_backupFolder, keepCount: 2);
        var remaining = await _service.GetRecentBackupsAsync(_backupFolder, 10);
        Assert.Equal(2, remaining.Count);
    }

    [Fact]
    public async Task BackupService_GetDatabaseSize_ReturnsPositiveValue()
    {
        var size = await _service.GetDatabaseSizeAsync();
        Assert.True(size > 0);
    }

    [Fact]
    public async Task BackupService_GetRecentBackups_EmptyFolder_ReturnsEmpty()
    {
        var list = await _service.GetRecentBackupsAsync(_backupFolder, 5);
        Assert.Empty(list);
    }
}

// ── CompanySettingsViewModel Integration Tests ────────────────────────────────

public class Phase13CompanyVmTests
{
    private static (CompanySettingsViewModel vm, Mock<ISettingsService> svc) Create()
    {
        var svc = new Mock<ISettingsService>();
        svc.Setup(s => s.LoadCompanySettingsAsync(default)).ReturnsAsync(new CompanySettings
        {
            CompanyName = "Test Store",
            GSTIN       = "27AAPFU0939F1ZV",
            Email       = "test@example.com"
        });
        svc.Setup(s => s.SaveCompanySettingsAsync(It.IsAny<CompanySettings>(), default))
           .Returns(Task.CompletedTask);
        return (new CompanySettingsViewModel(svc.Object), svc);
    }

    [Fact]
    public async Task CompanyVm_Load_PopulatesProperties()
    {
        var (vm, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Test Store", vm.CompanyName);
        Assert.Equal("27AAPFU0939F1ZV", vm.GSTIN);
        Assert.Equal("test@example.com", vm.Email);
    }

    [Fact]
    public async Task CompanyVm_SaveWithInvalidGstin_SetsHasError()
    {
        var (vm, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.GSTIN = "INVALID_GSTIN";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.False(string.IsNullOrEmpty(vm.StatusMessage));
    }

    [Fact]
    public async Task CompanyVm_SaveWithValidData_CallsService()
    {
        var (vm, svc) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.GSTIN = "27AAPFU0939F1ZV";
        vm.Email = "admin@store.com";

        await vm.SaveCommand.ExecuteAsync(null);

        svc.Verify(s => s.SaveCompanySettingsAsync(It.IsAny<CompanySettings>(), default), Times.Once);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task CompanyVm_Reset_RestoresDefaults()
    {
        var (vm, _) = Create();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.CompanyName = "Modified";

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.Equal("Gold Jewellery Store", vm.CompanyName);
    }
}

// ── TaxSettingsViewModel Tests ────────────────────────────────────────────────

public class Phase13TaxVmTests
{
    private static TaxSettingsViewModel Create()
    {
        var svc = new Mock<ISettingsService>();
        svc.Setup(s => s.LoadTaxSettingsAsync(default)).ReturnsAsync(new TaxSettings());
        svc.Setup(s => s.SaveTaxSettingsAsync(It.IsAny<TaxSettings>(), default)).Returns(Task.CompletedTask);
        return new TaxSettingsViewModel(svc.Object);
    }

    [Fact]
    public async Task TaxVm_SaveWithInvalidRate_SetsHasError()
    {
        var vm = Create();
        vm.CGSTRate = 150m; // invalid

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
    }

    [Fact]
    public async Task TaxVm_AllRatesValid_ReturnsTrueForDefaultRates()
    {
        var vm = Create();
        Assert.True(vm.AllRatesValid);
        await Task.CompletedTask;
    }
}
