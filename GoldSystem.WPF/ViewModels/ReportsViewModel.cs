using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using GoldSystem.WPF.Services;
using Microsoft.Win32;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Phase 12 – Reports coordinator ViewModel.
/// Four tabs: Day Book, Sales Register, Customer Ledger, GSTR-1.
/// </summary>
public sealed partial class ReportsViewModel : BaseViewModel
{
    private readonly IReportGenerationService _generator;
    private readonly IReportExportService _exporter;

    // ── Status ──────────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private int _selectedTabIndex;

    // ── Shared filters ───────────────────────────────────────────────────────
    [ObservableProperty] private DateTime _fromDate = DateTime.Today;
    [ObservableProperty] private DateTime _toDate   = DateTime.Today;
    [ObservableProperty] private int _selectedMonth = DateTime.Today.Month;
    [ObservableProperty] private int _selectedYear  = DateTime.Today.Year;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 1 – Day Book
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<DayBookLine> _dayBookLines = [];
    [ObservableProperty] private decimal _dayBookTotalAmount;
    [ObservableProperty] private decimal _dayBookTotalPaid;
    [ObservableProperty] private decimal _dayBookTotalOutstanding;
    [ObservableProperty] private int _dayBookBillCount;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 2 – Sales Register
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<SalesRegisterLine> _salesLines = [];
    [ObservableProperty] private decimal _salesTotalRevenue;
    [ObservableProperty] private decimal _salesTotalGrossWeight;
    [ObservableProperty] private decimal _salesTotalNetWeight;
    [ObservableProperty] private int _salesItemCount;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 3 – Customer Ledger
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<LedgerReportLine> _ledgerLines = [];
    [ObservableProperty] private ObservableCollection<AgeingBucket> _ageingBuckets = [];
    [ObservableProperty] private decimal _ledgerTotalOutstanding;
    [ObservableProperty] private int _ledgerCustomerCount;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 4 – GSTR-1
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private GSTR1Summary? _gstr1Summary;
    [ObservableProperty] private ObservableCollection<GSTR1Line> _gstr1Lines = [];
    [ObservableProperty] private decimal _gstr1TotalTaxable;
    [ObservableProperty] private decimal _gstr1TotalTax;
    [ObservableProperty] private int _gstr1InvoiceCount;

    public static IReadOnlyList<int> Months { get; } = Enumerable.Range(1, 12).ToList();
    public static IReadOnlyList<int> Years  { get; } = Enumerable.Range(DateTime.Today.Year - 3, 5).ToList();

    public ReportsViewModel(
        NavigationService navigation,
        AppState appState,
        IReportGenerationService generator,
        IReportExportService exporter)
        : base(navigation, appState)
    {
        _generator = generator;
        _exporter  = exporter;
    }

    // ─── Day Book commands ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task GenerateDayBookAsync()
    {
        await RunAsync("Generating Day Book…", async () =>
        {
            var date = DateOnly.FromDateTime(FromDate);
            var lines = await _generator.GenerateDayBookAsync(date, AppState.CurrentBranchId);
            DayBookLines = new ObservableCollection<DayBookLine>(lines);

            DayBookBillCount         = lines.Count;
            DayBookTotalAmount       = lines.Sum(l => l.Amount);
            DayBookTotalPaid         = lines.Sum(l => l.AmountPaid);
            DayBookTotalOutstanding  = DayBookTotalAmount - DayBookTotalPaid;
            StatusMessage = $"Day Book: {lines.Count} bill(s) for {date:dd-MMM-yyyy}";
        });
    }

    [RelayCommand]
    public async Task ExportDayBookToPdfAsync()
    {
        if (DayBookLines.Count == 0) { StatusMessage = "Generate the Day Book first."; return; }
        var bytes = _exporter.ExportDayBookToPdf(
            DayBookLines.ToList(), DateOnly.FromDateTime(FromDate), AppState.CurrentBranchName);
        await SaveFileAsync(bytes, "DayBook.pdf", "PDF Files|*.pdf");
    }

    [RelayCommand]
    public async Task ExportDayBookToExcelAsync()
    {
        if (DayBookLines.Count == 0) { StatusMessage = "Generate the Day Book first."; return; }
        var bytes = _exporter.ExportDayBookToExcel(DayBookLines.ToList(), DateOnly.FromDateTime(FromDate));
        await SaveFileAsync(bytes, "DayBook.xlsx", "Excel Files|*.xlsx");
    }

    // ─── Sales Register commands ──────────────────────────────────────────────

    [RelayCommand]
    public async Task GenerateSalesRegisterAsync()
    {
        await RunAsync("Generating Sales Register…", async () =>
        {
            var from = DateOnly.FromDateTime(FromDate);
            var to   = DateOnly.FromDateTime(ToDate);
            var lines = await _generator.GenerateSalesRegisterAsync(from, to, AppState.CurrentBranchId);
            SalesLines = new ObservableCollection<SalesRegisterLine>(lines);

            SalesItemCount        = lines.Count;
            SalesTotalRevenue     = lines.Sum(l => l.Revenue);
            SalesTotalGrossWeight = lines.Sum(l => l.GrossWeight);
            SalesTotalNetWeight   = lines.Sum(l => l.NetWeight);
            StatusMessage = $"Sales Register: {lines.Count} line(s)";
        });
    }

    [RelayCommand]
    public async Task ExportSalesRegisterToPdfAsync()
    {
        if (SalesLines.Count == 0) { StatusMessage = "Generate the Sales Register first."; return; }
        var bytes = _exporter.ExportSalesRegisterToPdf(
            SalesLines.ToList(),
            DateOnly.FromDateTime(FromDate),
            DateOnly.FromDateTime(ToDate),
            AppState.CurrentBranchName);
        await SaveFileAsync(bytes, "SalesRegister.pdf", "PDF Files|*.pdf");
    }

    [RelayCommand]
    public async Task ExportSalesRegisterToExcelAsync()
    {
        if (SalesLines.Count == 0) { StatusMessage = "Generate the Sales Register first."; return; }
        var bytes = _exporter.ExportSalesRegisterToExcel(
            SalesLines.ToList(),
            DateOnly.FromDateTime(FromDate),
            DateOnly.FromDateTime(ToDate));
        await SaveFileAsync(bytes, "SalesRegister.xlsx", "Excel Files|*.xlsx");
    }

    // ─── Ledger commands ──────────────────────────────────────────────────────

    [RelayCommand]
    public async Task GenerateLedgerAsync()
    {
        await RunAsync("Generating Customer Ledger…", async () =>
        {
            var lines  = await _generator.GenerateLedgerReportAsync(AppState.CurrentBranchId);
            var ageing = await _generator.GetLedgerAgeingAsync(AppState.CurrentBranchId);

            LedgerLines     = new ObservableCollection<LedgerReportLine>(lines);
            AgeingBuckets   = new ObservableCollection<AgeingBucket>(ageing);

            LedgerCustomerCount    = lines.Count;
            LedgerTotalOutstanding = lines.Sum(l => l.OutstandingAmount);
            StatusMessage = $"Ledger: {lines.Count} customer(s) with outstanding";
        });
    }

    [RelayCommand]
    public async Task ExportLedgerToPdfAsync()
    {
        if (LedgerLines.Count == 0) { StatusMessage = "Generate the Ledger first."; return; }
        var bytes = _exporter.ExportLedgerToPdf(
            LedgerLines.ToList(), AgeingBuckets.ToList(), AppState.CurrentBranchName);
        await SaveFileAsync(bytes, "CustomerLedger.pdf", "PDF Files|*.pdf");
    }

    [RelayCommand]
    public async Task ExportLedgerToExcelAsync()
    {
        if (LedgerLines.Count == 0) { StatusMessage = "Generate the Ledger first."; return; }
        var bytes = _exporter.ExportLedgerToExcel(LedgerLines.ToList(), AgeingBuckets.ToList());
        await SaveFileAsync(bytes, "CustomerLedger.xlsx", "Excel Files|*.xlsx");
    }

    // ─── GSTR-1 commands ─────────────────────────────────────────────────────

    [RelayCommand]
    public async Task GenerateGSTR1Async()
    {
        await RunAsync($"Generating GSTR-1 for {SelectedMonth:D2}/{SelectedYear}…", async () =>
        {
            var summary = await _generator.GenerateGSTR1Async(
                SelectedMonth, SelectedYear, AppState.CurrentBranchId);

            Gstr1Summary      = summary;
            Gstr1Lines        = new ObservableCollection<GSTR1Line>(summary.Invoices);
            Gstr1InvoiceCount = summary.Invoices.Count;
            Gstr1TotalTaxable = summary.TotalTaxable;
            Gstr1TotalTax     = summary.TotalTax;
            StatusMessage = $"GSTR-1: {summary.Invoices.Count} invoice(s) | Tax: ₹{summary.TotalTax:N0}";
        });
    }

    [RelayCommand]
    public async Task ExportGSTR1ToJsonAsync()
    {
        if (Gstr1Summary is null) { StatusMessage = "Generate GSTR-1 first."; return; }
        var json  = _exporter.ExportGSTR1ToJson(Gstr1Summary);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        await SaveFileAsync(bytes, $"GSTR1_{SelectedYear}_{SelectedMonth:D2}.json", "JSON Files|*.json");
    }

    [RelayCommand]
    public async Task ExportGSTR1ToExcelAsync()
    {
        if (Gstr1Summary is null) { StatusMessage = "Generate GSTR-1 first."; return; }
        var bytes = _exporter.ExportGSTR1ToExcel(Gstr1Summary);
        await SaveFileAsync(bytes, $"GSTR1_{SelectedYear}_{SelectedMonth:D2}.xlsx", "Excel Files|*.xlsx");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task RunAsync(string loadingMsg, Func<Task> action)
    {
        IsLoading     = true;
        HasError      = false;
        StatusMessage = loadingMsg;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            HasError      = true;
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static Task SaveFileAsync(byte[] bytes, string defaultFileName, string filter)
    {
        var dlg = new SaveFileDialog
        {
            FileName = defaultFileName,
            Filter   = filter,
        };

        if (dlg.ShowDialog() == true)
            File.WriteAllBytes(dlg.FileName, bytes);

        return Task.CompletedTask;
    }
}
