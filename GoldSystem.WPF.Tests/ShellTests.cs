using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Services;
using GoldSystem.Reports.Services;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.ViewModels;
using GoldSystem.RateEngine.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GoldSystem.WPF.Tests;

/// <summary>
/// Phase 9 unit tests: AppState, ThemeService, NavigationService,
/// StatusIndicatorService, ShellViewModel, DashboardViewModel, and all scaffolded ViewModels.
/// </summary>
public class AppStateTests
{
    [Fact]
    public void AppState_DefaultValues_AreCorrect()
    {
        var state = new AppState();
        Assert.Equal(1, state.CurrentBranchId);
        Assert.Equal("Default Branch", state.CurrentBranchName);
        Assert.Equal("Admin", state.CurrentUserName);
        Assert.True(state.IsOwnerBranch);
        Assert.True(state.IsOnline);
        Assert.Equal(0, state.PendingSyncCount);
    }

    [Fact]
    public void AppState_UpdateRates_SetsAllRatesAndSource()
    {
        var state = new AppState();
        state.UpdateRates(75000m, 68750m, 56250m, "MCX");

        Assert.Equal(75000m, state.CurrentRate24K);
        Assert.Equal(68750m, state.CurrentRate22K);
        Assert.Equal(56250m, state.CurrentRate18K);
        Assert.Equal("MCX", state.RateSource);
        Assert.NotEqual(DateTime.MinValue, state.RateUpdatedAt);
    }

    [Fact]
    public void AppState_Rate22KDisplay_FormatsCorrectly()
    {
        var state = new AppState();
        state.UpdateRates(75000m, 68750m, 56250m, "MCX");

        Assert.Contains("22K", state.Rate22KDisplay);
        Assert.Contains("68,750", state.Rate22KDisplay);
    }

    [Fact]
    public void AppState_Rate24KDisplay_WhenZero_ShowsDash()
    {
        var state = new AppState();
        Assert.Equal("24K: –", state.Rate24KDisplay);
    }

    [Fact]
    public void AppState_Rate18KDisplay_FormatsCorrectly()
    {
        var state = new AppState();
        state.UpdateRates(75000m, 68750m, 56250m, "MCX");

        Assert.Contains("18K", state.Rate18KDisplay);
        Assert.Contains("56,250", state.Rate18KDisplay);
    }

    [Fact]
    public void AppState_BranchProperties_UpdateAndNotify()
    {
        var state = new AppState();
        bool notified = false;
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.CurrentBranchName)) notified = true;
        };

        state.CurrentBranchName = "Mumbai";
        Assert.Equal("Mumbai", state.CurrentBranchName);
        Assert.True(notified);
    }

    [Fact]
    public void AppState_PendingSyncCount_Notifies()
    {
        var state = new AppState();
        bool notified = false;
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.PendingSyncCount)) notified = true;
        };
        state.PendingSyncCount = 5;
        Assert.Equal(5, state.PendingSyncCount);
        Assert.True(notified);
    }
}

public class ThemeServiceTests
{
    [Fact]
    public void ThemeService_DefaultIsDarkMode_IsFalse()
    {
        var svc = new ThemeService();
        Assert.False(svc.IsDarkMode);
    }

    [Fact]
    public void ThemeService_SetDarkMode_True_SetsIsDarkMode()
    {
        var svc = new ThemeService();
        svc.SetDarkMode(true);
        Assert.True(svc.IsDarkMode);
    }

    [Fact]
    public void ThemeService_ToggleTheme_FlipsMode()
    {
        var svc = new ThemeService();
        Assert.False(svc.IsDarkMode);
        svc.ToggleTheme();
        Assert.True(svc.IsDarkMode);
        svc.ToggleTheme();
        Assert.False(svc.IsDarkMode);
    }

    [Fact]
    public void ThemeService_ApplyLightTheme_SetsFalse()
    {
        var svc = new ThemeService();
        svc.SetDarkMode(true);
        svc.ApplyLightTheme();
        Assert.False(svc.IsDarkMode);
    }

    [Fact]
    public void ThemeService_ApplyDarkTheme_SetsTrue()
    {
        var svc = new ThemeService();
        svc.ApplyDarkTheme();
        Assert.True(svc.IsDarkMode);
    }
}

public class StatusIndicatorServiceTests
{
    private static (StatusIndicatorService svc, AppState state) CreateService()
    {
        var appState = new AppState();
        var publisher = new RateChangedEventPublisher(NullLogger<RateChangedEventPublisher>.Instance);
        var logger = NullLogger<StatusIndicatorService>.Instance;
        var svc = new StatusIndicatorService(appState, publisher, logger);
        return (svc, appState);
    }

    [Fact]
    public void StatusIndicator_DefaultStatus_IsIdle()
    {
        var (svc, _) = CreateService();
        Assert.Equal("Idle", svc.SyncStatus);
        Assert.Equal("CheckCircle", svc.SyncStatusIcon);
        Assert.False(svc.IsSyncing);
    }

    [Fact]
    public void StatusIndicator_ReportSyncStarted_SetsSyncingTrue()
    {
        var (svc, _) = CreateService();
        svc.ReportSyncStarted();
        Assert.True(svc.IsSyncing);
        Assert.Equal("Sync", svc.SyncStatusIcon);
    }

    [Fact]
    public void StatusIndicator_ReportSyncCompleted_WithRecords_ShowsCount()
    {
        var (svc, _) = CreateService();
        svc.ReportSyncStarted();
        svc.ReportSyncCompleted(12);
        Assert.False(svc.IsSyncing);
        Assert.Contains("12", svc.SyncStatus);
        Assert.Equal("CheckCircle", svc.SyncStatusIcon);
    }

    [Fact]
    public void StatusIndicator_ReportSyncCompleted_Zero_ShowsUpToDate()
    {
        var (svc, _) = CreateService();
        svc.ReportSyncCompleted(0);
        Assert.Equal("Up to date", svc.SyncStatus);
    }

    [Fact]
    public void StatusIndicator_ReportSyncFailed_SetsErrorIcon()
    {
        var (svc, _) = CreateService();
        svc.ReportSyncFailed("Connection refused");
        Assert.False(svc.IsSyncing);
        Assert.Equal("AlertCircle", svc.SyncStatusIcon);
        Assert.Contains("Connection refused", svc.SyncStatus);
    }

    [Fact]
    public void StatusIndicator_ReportOffline_SetsOfflineState()
    {
        var (svc, appState) = CreateService();
        svc.ReportOffline(45);
        Assert.False(svc.IsSyncing);
        Assert.Equal("CloudOff", svc.SyncStatusIcon);
        Assert.Equal(45, appState.PendingSyncCount);
        Assert.False(appState.IsOnline);
    }

    [Fact]
    public void StatusIndicator_Dispose_DoesNotThrow()
    {
        var (svc, _) = CreateService();
        var ex = Record.Exception(() => svc.Dispose());
        Assert.Null(ex);
    }
}

public class ShellViewModelTests
{
    private static (ShellViewModel vm, NavigationService nav) CreateShellVm()
    {
        var sp = new Mock<IServiceProvider>();
        var nav = new NavigationService(sp.Object);
        var state = new AppState();
        var publisher = new RateChangedEventPublisher(NullLogger<RateChangedEventPublisher>.Instance);
        var logger = NullLogger<StatusIndicatorService>.Instance;
        var statusSvc = new StatusIndicatorService(state, publisher, logger);
        var theme = new ThemeService();
        var vm = new ShellViewModel(nav, state, statusSvc, theme);
        return (vm, nav);
    }

    [Fact]
    public void ShellViewModel_DefaultSelectedMenuItem_IsDashboard()
    {
        var (vm, _) = CreateShellVm();
        Assert.Equal("Dashboard", vm.SelectedMenuItem);
    }

    [Fact]
    public void ShellViewModel_IsSidebarExpandedByDefault()
    {
        var (vm, _) = CreateShellVm();
        Assert.True(vm.IsSidebarExpanded);
    }

    [Fact]
    public void ShellViewModel_ToggleSidebarCommand_CollapsesSidebar()
    {
        var (vm, _) = CreateShellVm();
        vm.ToggleSidebarCommand.Execute(null);
        Assert.False(vm.IsSidebarExpanded);
    }

    [Fact]
    public void ShellViewModel_ToggleSidebarCommand_TogglesTwice()
    {
        var (vm, _) = CreateShellVm();
        vm.ToggleSidebarCommand.Execute(null);
        vm.ToggleSidebarCommand.Execute(null);
        Assert.True(vm.IsSidebarExpanded);
    }

    [Fact]
    public void ShellViewModel_ToggleThemeCommand_DoesNotThrow()
    {
        var (vm, _) = CreateShellVm();
        var ex = Record.Exception(() => vm.ToggleThemeCommand.Execute(null));
        Assert.Null(ex);
    }

    [Fact]
    public void ShellViewModel_AppStateExposed()
    {
        var (vm, _) = CreateShellVm();
        Assert.NotNull(vm.AppState);
    }

    [Fact]
    public void ShellViewModel_StatusIndicatorExposed()
    {
        var (vm, _) = CreateShellVm();
        Assert.NotNull(vm.StatusIndicator);
    }

    [Fact]
    public void ShellViewModel_NavigationService_CanNavigateBackFalseInitially()
    {
        var (_, nav) = CreateShellVm();
        Assert.False(nav.CanNavigateBack);
    }

    [Fact]
    public void ShellViewModel_SelectedMenuItem_ChangesOnNavigate()
    {
        var (vm, _) = CreateShellVm();
        vm.SelectedMenuItem = "Billing";
        Assert.Equal("Billing", vm.SelectedMenuItem);
    }
}

public class ScaffoldedViewModelTests
{
    private static NavigationService CreateNav()
    {
        var sp = new Mock<IServiceProvider>();
        return new NavigationService(sp.Object);
    }

    private static AppState CreateState() => new AppState();

    [Fact]
    public void BillingViewModel_Instantiates()
    {
        var vm = BillingViewModelFactory.Create(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public async Task BillingViewModel_LoadAsync_SetsIsLoadingFalse()
    {
        var vm = BillingViewModelFactory.Create(CreateNav(), CreateState());
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void InventoryViewModel_Instantiates()
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Items.GetInventoryByBranchAsync(It.IsAny<int>(), default))
               .ReturnsAsync([]);
        mockUow.Setup(u => u.Branches.GetActiveBranchesAsync(default)).ReturnsAsync([]);
        var auditMock = new Mock<GoldSystem.Core.Services.IAuditLogger>();
        var svc = new GoldSystem.Data.Services.StockTransferService(mockUow.Object, auditMock.Object);
        var vm = new InventoryViewModel(CreateNav(), CreateState(), mockUow.Object, svc,
                                        NullLogger<InventoryViewModel>.Instance);
        Assert.NotNull(vm);
    }

    [Fact]
    public void CustomerViewModel_Instantiates()
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Customers.GetAllAsync(default)).ReturnsAsync([]);
        mockUow.Setup(u => u.Customers.GetTopCustomersByVolumeAsync(It.IsAny<int>(), It.IsAny<int>(), default))
               .ReturnsAsync([]);
        var auditMock = new Mock<GoldSystem.Core.Services.IAuditLogger>();
        var svc = new GoldSystem.Data.Services.StockTransferService(mockUow.Object, auditMock.Object);
        var vm = new CustomerViewModel(CreateNav(), CreateState(), mockUow.Object, svc,
                                       NullLogger<CustomerViewModel>.Instance);
        Assert.NotNull(vm);
    }

    [Fact]
    public void GoldRateViewModel_Instantiates()
    {
        var vm = new GoldRateViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void ReportsViewModel_Instantiates()
    {
        var generator = new Mock<GoldSystem.Core.Interfaces.IReportGenerationService>();
        var exporter  = new Mock<GoldSystem.Core.Interfaces.IReportExportService>();
        var vm = new ReportsViewModel(CreateNav(), CreateState(), generator.Object, exporter.Object);
        Assert.NotNull(vm);
    }

    private static SettingsViewModel CreateSettingsVm(ThemeService? theme = null)
    {
        var ts   = theme ?? new ThemeService();
        var svc  = new Mock<GoldSystem.Core.Interfaces.ISettingsService>();
        var bsvc = new Mock<GoldSystem.Core.Interfaces.IBackupService>();
        svc.Setup(s => s.LoadCompanySettingsAsync(default)).ReturnsAsync(new GoldSystem.Core.Models.CompanySettings());
        svc.Setup(s => s.LoadTaxSettingsAsync(default)).ReturnsAsync(new GoldSystem.Core.Models.TaxSettings());
        svc.Setup(s => s.LoadThemeSettingsAsync(default)).ReturnsAsync(new GoldSystem.Core.Models.ThemeSettings());
        svc.Setup(s => s.LoadBackupSettingsAsync(default)).ReturnsAsync(new GoldSystem.Core.Models.BackupSettings());
        svc.Setup(s => s.LoadUserPreferencesAsync(default)).ReturnsAsync(new GoldSystem.Core.Models.UserPreferences());
        svc.Setup(s => s.LoadAdvancedSettingsAsync(default)).ReturnsAsync(new GoldSystem.Core.Models.AdvancedSettings());
        bsvc.Setup(b => b.GetDatabaseSizeAsync(default)).ReturnsAsync(0L);
        return new SettingsViewModel(
            CreateNav(), CreateState(),
            new CompanySettingsViewModel(svc.Object),
            new TaxSettingsViewModel(svc.Object),
            new ThemeSettingsViewModel(svc.Object, ts),
            new BackupSettingsViewModel(svc.Object, bsvc.Object),
            new UserPreferencesViewModel(svc.Object),
            new AdvancedSettingsViewModel(svc.Object, bsvc.Object));
    }

    [Fact]
    public void SettingsViewModel_IsDarkMode_DefaultFalse()
    {
        var vm = CreateSettingsVm();
        Assert.False(vm.Theme.IsDarkMode);
    }

    [Fact]
    public void SettingsViewModel_ToggleDarkMode_UpdatesTheme()
    {
        var theme = new ThemeService();
        var vm = CreateSettingsVm(theme);
        vm.Theme.IsDarkMode = true;
        Assert.True(theme.IsDarkMode);
    }

    [Fact]
    public void VendorViewModel_Instantiates()
    {
        var vm = new VendorViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void CategoryViewModel_Instantiates()
    {
        var vm = new CategoryViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void SyncStatusViewModel_Instantiates()
    {
        var vm = new SyncStatusViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void AIInsightsViewModel_Instantiates()
    {
        var vm = new AIInsightsViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void AuditLogViewModel_Instantiates()
    {
        var vm = new AuditLogViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void UserManagementViewModel_Instantiates()
    {
        var vm = new UserManagementViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void BranchViewModel_Instantiates()
    {
        var vm = new BranchViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
    }

    [Fact]
    public void AboutViewModel_Instantiates()
    {
        var vm = new AboutViewModel(CreateNav(), CreateState());
        Assert.NotNull(vm);
        Assert.Equal("1.0.0", vm.AppVersion);
        Assert.Contains("Gold", vm.AppTitle);
    }
}

/// <summary>
/// Factory helper for creating BillingViewModel with mocked dependencies in tests.
/// </summary>
internal static class BillingViewModelFactory
{
    public static BillingViewModel Create(NavigationService nav, AppState appState)
    {
        var mockUow = new Mock<IUnitOfWork>();
        mockUow.Setup(u => u.Customers.GetAllAsync(default)).ReturnsAsync([]);
        mockUow.Setup(u => u.GoldRates.GetLatestRateAsync(It.IsAny<int>(), default))
               .ReturnsAsync((GoldRate?)null);

        var screenService = new BillingScreenService(mockUow.Object);
        var billingEngineMock = new Mock<IBillingEngine>().Object;
        var pdfMock = new Mock<IBillPdfService>().Object;
        var calc = new GoldPriceCalculator();
        var logger = NullLogger<BillingViewModel>.Instance;

        return new BillingViewModel(nav, appState, billingEngineMock, calc, screenService, pdfMock, logger);
    }
}
