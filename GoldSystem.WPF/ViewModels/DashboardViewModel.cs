using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Data;
using GoldSystem.Data.Repositories;
using GoldSystem.WPF.Services;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Dashboard ViewModel – loads KPI cards for the home screen.
/// Charts are injected at runtime by DashboardView code-behind.
/// </summary>
public sealed partial class DashboardViewModel : BaseViewModel
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<DashboardViewModel> _logger;

    // ── KPI Properties ──────────────────────────────────────────────────────

    [ObservableProperty] private decimal _todaySales;
    [ObservableProperty] private int _todayBillCount;
    [ObservableProperty] private decimal _monthSales;
    [ObservableProperty] private int _monthBillCount;
    [ObservableProperty] private int _totalItemsInStock;
    [ObservableProperty] private int _lowStockAlerts;
    [ObservableProperty] private int _pendingSyncCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingError = string.Empty;

    // ── Chart Data (returned as plain collections for code-behind chart injection) ──

    public List<(string Label, decimal Value)> WeeklySalesData { get; private set; } = new();
    public List<(string Label, decimal Value)> RateTrendData { get; private set; } = new();

    public DashboardViewModel(
        NavigationService navigation,
        AppState appState,
        IUnitOfWork uow,
        ILogger<DashboardViewModel> logger)
        : base(navigation, appState)
    {
        _uow = uow;
        _logger = logger;
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        LoadingError = string.Empty;

        try
        {
            await LoadKpisAsync();
            await BuildChartDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard data load failed.");
            LoadingError = "Could not load dashboard data. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadKpisAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var allBills = await _uow.Bills.GetAllAsync();
        var billList = allBills.ToList();

        TodayBillCount = billList.Count(b => b.BillDate == today);
        TodaySales = billList.Where(b => b.BillDate == today).Sum(b => b.GrandTotal);

        MonthBillCount = billList.Count(b => b.BillDate >= monthStart);
        MonthSales = billList.Where(b => b.BillDate >= monthStart).Sum(b => b.GrandTotal);

        var allItems = await _uow.Items.GetAllAsync();
        var items = allItems.ToList();
        TotalItemsInStock = items.Count(i => i.Status == "InStock");
        LowStockAlerts = 0;

        var syncItems = await _uow.SyncQueue.GetAllAsync();
        PendingSyncCount = syncItems.Count(s => s.Status == "Pending");
        AppState.PendingSyncCount = PendingSyncCount;
    }

    private async Task BuildChartDataAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var allBills = await _uow.Bills.GetAllAsync();
        var bills = allBills.ToList();

        WeeklySalesData = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date = today.AddDays(-(6 - i));
                return (date.ToString("ddd"), bills.Where(b => b.BillDate == date).Sum(b => b.GrandTotal));
            })
            .ToList();

        var branchId = AppState.CurrentBranchId;
        var rates = await _uow.GoldRates.GetRateHistoryAsync(branchId, 30);
        RateTrendData = rates.OrderBy(r => r.RateDate)
            .Select(r => (r.RateDate.ToString("dd/MM"), r.Rate22K))
            .ToList();
    }
}

