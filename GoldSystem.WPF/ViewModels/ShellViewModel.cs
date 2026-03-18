using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.WPF.Services;
using GoldSystem.WPF.Views;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Shell (main window) ViewModel – coordinates the sidebar navigation,
/// header bar (rates display) and content area.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly AppState _appState;
    private readonly StatusIndicatorService _statusIndicator;
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private string _selectedMenuItem = "Dashboard";

    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    public AppState AppState => _appState;
    public StatusIndicatorService StatusIndicator => _statusIndicator;

    public ShellViewModel(
        INavigationService navigation,
        AppState appState,
        StatusIndicatorService statusIndicator,
        ThemeService themeService)
    {
        _navigation = navigation;
        _appState = appState;
        _statusIndicator = statusIndicator;
        _themeService = themeService;
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        SelectedMenuItem = "Dashboard";
        _navigation.NavigateTo<DashboardView>();
    }

    [RelayCommand]
    private void NavigateToBilling()
    {
        SelectedMenuItem = "Billing";
        _navigation.NavigateTo<BillingView>();
    }

    [RelayCommand]
    private void NavigateToInventory()
    {
        SelectedMenuItem = "Inventory";
        _navigation.NavigateTo<InventoryView>();
    }

    [RelayCommand]
    private void NavigateToCustomers()
    {
        SelectedMenuItem = "Customers";
        _navigation.NavigateTo<CustomerView>();
    }

    [RelayCommand]
    private void NavigateToGoldRates()
    {
        SelectedMenuItem = "Gold Rates";
        _navigation.NavigateTo<GoldRateView>();
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        SelectedMenuItem = "Reports";
        _navigation.NavigateTo<ReportsView>();
    }

    [RelayCommand]
    private void NavigateToVendors()
    {
        SelectedMenuItem = "Vendors";
        _navigation.NavigateTo<VendorView>();
    }

    [RelayCommand]
    private void NavigateToCategories()
    {
        SelectedMenuItem = "Categories";
        _navigation.NavigateTo<CategoryView>();
    }

    [RelayCommand]
    private void NavigateToSyncStatus()
    {
        SelectedMenuItem = "Sync Status";
        _navigation.NavigateTo<SyncStatusView>();
    }

    [RelayCommand]
    private void NavigateToAIInsights()
    {
        SelectedMenuItem = "AI Insights";
        _navigation.NavigateTo<AIInsightsView>();
    }

    [RelayCommand]
    private void NavigateToAuditLog()
    {
        SelectedMenuItem = "Audit Log";
        _navigation.NavigateTo<AuditLogView>();
    }

    [RelayCommand]
    private void NavigateToUserManagement()
    {
        SelectedMenuItem = "Users";
        _navigation.NavigateTo<UserManagementView>();
    }

    [RelayCommand]
    private void NavigateToBranches()
    {
        SelectedMenuItem = "Branches";
        _navigation.NavigateTo<BranchView>();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SelectedMenuItem = "Settings";
        _navigation.NavigateTo<SettingsView>();
    }

    [RelayCommand]
    private void NavigateToAbout()
    {
        SelectedMenuItem = "About";
        _navigation.NavigateTo<AboutView>();
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }
}
