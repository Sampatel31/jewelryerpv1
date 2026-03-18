using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data.Services;
using GoldSystem.Reports.Services;
using GoldSystem.WPF.Services;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Phase 10 – full billing screen ViewModel.
/// Handles item scanning, real-time calculations, customer selection,
/// 6-mode payment processing, bill saving (atomic) and PDF printing.
/// </summary>
public sealed partial class BillingViewModel : BaseViewModel
{
    private readonly IBillingEngine _billingEngine;
    private readonly GoldPriceCalculator _calculator;
    private readonly BillingScreenService _screenService;
    private readonly IBillPdfService _pdfService;
    private readonly ILogger<BillingViewModel> _logger;

    // ── Loading / status ────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    // ── Customer selection ──────────────────────────────────────────────────
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private ObservableCollection<CustomerDto> _customerSuggestions = [];
    [ObservableProperty] private bool _isCustomerSearchOpen;

    // ── Barcode / tag scan ──────────────────────────────────────────────────
    [ObservableProperty] private string _barcodeInput = string.Empty;
    [ObservableProperty] private string _scanStatusMessage = string.Empty;

    // ── Line items ──────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<BillLineItemViewModel> _lineItems = [];
    [ObservableProperty] private BillLineItemViewModel? _selectedLineItem;

    // ── Calculation panel (real-time) ────────────────────────────────────────
    [ObservableProperty] private decimal _goldValueTotal;
    [ObservableProperty] private decimal _makingAmountTotal;
    [ObservableProperty] private decimal _wastageAmountTotal;
    [ObservableProperty] private decimal _stoneChargeTotal;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _cgst;
    [ObservableProperty] private decimal _sgst;
    [ObservableProperty] private decimal _igst;
    [ObservableProperty] private decimal _roundOff;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private decimal _exchangeValue;
    [ObservableProperty] private decimal _amountPaid;
    [ObservableProperty] private decimal _balanceDue;
    [ObservableProperty] private string _paymentStatus = "Pending";

    // ── Payment mode ────────────────────────────────────────────────────────
    [ObservableProperty] private string _selectedPaymentMode = "Cash";
    public static IReadOnlyList<string> PaymentModes { get; } =
        ["Cash", "Card", "UPI", "NEFT", "Split", "OldGoldExchange"];

    // ── Saved bill reference ─────────────────────────────────────────────────
    [ObservableProperty] private BillDto? _savedBill;
    [ObservableProperty] private bool _isBillSaved;

    // ── Rate display ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _currentRateDisplay = "Rate: –";

    public BillingViewModel(
        INavigationService navigation,
        AppState appState,
        IBillingEngine billingEngine,
        GoldPriceCalculator calculator,
        BillingScreenService screenService,
        IBillPdfService pdfService,
        ILogger<BillingViewModel> logger)
        : base(navigation, appState)
    {
        _billingEngine = billingEngine;
        _calculator = calculator;
        _screenService = screenService;
        _pdfService = pdfService;
        _logger = logger;
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    public override Task OnNavigatedToAsync()
    {
        ResetBill();
        RefreshRateDisplay();
        return Task.CompletedTask;
    }

    // ─── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Initial load (kept for compatibility with existing tests).</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        await Task.Delay(1); // yield to let UI render
        RefreshRateDisplay();
        IsLoading = false;
    }

    /// <summary>Search customers as user types in the search box.</summary>
    [RelayCommand]
    private async Task SearchCustomersAsync()
    {
        if (CustomerSearchText.Length < 2)
        {
            CustomerSuggestions.Clear();
            IsCustomerSearchOpen = false;
            return;
        }

        try
        {
            var results = await _screenService.SearchCustomersAsync(
                CustomerSearchText, AppState.CurrentBranchId);
            CustomerSuggestions = new ObservableCollection<CustomerDto>(results);
            IsCustomerSearchOpen = CustomerSuggestions.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer search failed");
            CustomerSuggestions.Clear();
        }
    }

    /// <summary>Called when user selects a customer from the autocomplete list.</summary>
    [RelayCommand]
    private void SelectCustomer(CustomerDto customer)
    {
        SelectedCustomer = customer;
        CustomerSearchText = $"{customer.Name} ({customer.Phone})";
        CustomerSuggestions.Clear();
        IsCustomerSearchOpen = false;
        HasError = false;
        StatusMessage = $"Customer: {customer.Name}";
    }

    /// <summary>Scan an item by barcode / tag number (also bound to Enter key on scan input).</summary>
    [RelayCommand]
    private async Task ScanItemAsync()
    {
        var tag = BarcodeInput.Trim();
        if (string.IsNullOrEmpty(tag)) return;

        // Prevent duplicate items
        if (LineItems.Any(l => l.TagNo.Equals(tag, StringComparison.OrdinalIgnoreCase) ||
                                l.Huid.Equals(tag, StringComparison.OrdinalIgnoreCase)))
        {
            ScanStatusMessage = $"⚠ Item '{tag}' already added to bill";
            BarcodeInput = string.Empty;
            return;
        }

        try
        {
            var item = await _screenService.LookupItemAsync(tag, AppState.CurrentBranchId);
            if (item is null)
            {
                ScanStatusMessage = $"✗ Item '{tag}' not found or not in stock";
                BarcodeInput = string.Empty;
                return;
            }

            var lineItem = BuildLineItem(item);
            LineItems.Add(lineItem);
            RecalculateTotals();
            BarcodeInput = string.Empty;
            ScanStatusMessage = $"✓ Added: {item.Name} ({item.Purity}) – ₹{lineItem.LineTotal:N0}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Item scan failed for tag {Tag}", tag);
            ScanStatusMessage = $"✗ Error scanning item: {ex.Message}";
            BarcodeInput = string.Empty;
        }
    }

    /// <summary>Remove selected line item from the bill.</summary>
    [RelayCommand]
    private void RemoveLineItem(BillLineItemViewModel? item)
    {
        if (item is null) return;
        LineItems.Remove(item);
        if (SelectedLineItem == item) SelectedLineItem = null;
        RecalculateTotals();
        ScanStatusMessage = $"Removed: {item.ItemName}";
    }

    /// <summary>Clear all line items and reset the bill.</summary>
    [RelayCommand]
    private void ClearBill()
    {
        ResetBill();
        StatusMessage = "Bill cleared";
    }

    /// <summary>Save bill with atomic transaction via IBillingEngine.</summary>
    [RelayCommand(CanExecute = nameof(CanSaveBill))]
    private async Task SaveBillAsync()
    {
        if (!ValidateForSave(out var validationError))
        {
            HasError = true;
            StatusMessage = validationError;
            return;
        }

        IsLoading = true;
        HasError = false;
        StatusMessage = "Saving bill…";

        try
        {
            var request = BuildCreateBillRequest();
            SavedBill = await _billingEngine.CreateBillAsync(request);
            IsBillSaved = true;
            StatusMessage = $"✓ Bill {SavedBill.BillNo} saved successfully!";
            SaveBillCommand.NotifyCanExecuteChanged();
            PrintBillCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"✗ Save failed: {ex.Message}";
            _logger.LogError(ex, "Bill save failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSaveBill() => !IsBillSaved && LineItems.Count > 0 && SelectedCustomer is not null;

    /// <summary>Print PDF bill. Locks the bill after printing.</summary>
    [RelayCommand(CanExecute = nameof(CanPrintBill))]
    private async Task PrintBillAsync()
    {
        if (SavedBill is null) return;

        IsLoading = true;
        StatusMessage = "Generating PDF…";

        try
        {
            var pdfBytes = _pdfService.GenerateBillPdf(
                SavedBill,
                shopName: AppState.CurrentBranchName,
                shopAddress: "Jewelry ERP System",
                shopPhone: "1800-GOLD-ERP");

            var tmpPath = Path.Combine(Path.GetTempPath(), $"Bill_{SavedBill.BillNo}.pdf");
            await File.WriteAllBytesAsync(tmpPath, pdfBytes);

            // Open with default PDF viewer
            Process.Start(new ProcessStartInfo(tmpPath) { UseShellExecute = true });

            // Lock the bill
            await _billingEngine.PrintBillAsync(SavedBill.BillId, AppState.CurrentUserId);

            StatusMessage = $"✓ PDF saved and bill locked: {tmpPath}";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"✗ Print failed: {ex.Message}";
            _logger.LogError(ex, "Bill print failed for bill {BillId}", SavedBill?.BillId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanPrintBill() => IsBillSaved && SavedBill is not null;

    /// <summary>Start a new bill after the current one is saved.</summary>
    [RelayCommand]
    private void NewBill()
    {
        ResetBill();
        StatusMessage = "New bill started";
    }

    // ─── Property-change handlers ─────────────────────────────────────────────

    partial void OnDiscountAmountChanged(decimal value) => RecalculateTotals();
    partial void OnExchangeValueChanged(decimal value) => RecalculateTotals();
    partial void OnAmountPaidChanged(decimal value) => RecalculateTotals();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void RefreshRateDisplay()
    {
        CurrentRateDisplay = AppState.CurrentRate24K > 0
            ? $"24K: ₹{AppState.CurrentRate24K:N0}/10g | 22K: ₹{AppState.CurrentRate22K:N0}/10g"
            : "Rate: Not available";
    }

    private BillLineItemViewModel BuildLineItem(ItemDto item)
    {
        var rate24K = AppState.CurrentRate24K > 0
            ? AppState.CurrentRate24K
            : 75000m; // fallback for UI display when no live rate

        var input = new GoldPriceCalculator.BillLineInput(
            GrossWeight: item.GrossWeight,
            StoneWeight: item.StoneWeight,
            Purity: item.Purity,
            MakingType: "PERCENT",   // defaults; items carry their own values
            MakingValue: 12m,
            WastagePercent: 2m,
            StoneCharge: 0m,
            Rate24KPer10g: rate24K);

        var result = _calculator.Calculate(input);

        return new BillLineItemViewModel
        {
            ItemId = item.ItemId,
            TagNo = item.TagNo,
            Huid = item.HUID,
            ItemName = item.Name,
            Purity = item.Purity,
            GrossWeight = item.GrossWeight,
            StoneWeight = item.StoneWeight,
            NetWeight = result.NetWeight,
            WastagePercent = input.WastagePercent,
            WastageWeight = result.WastageWeight,
            BillableWeight = result.NetWeight + result.WastageWeight,
            PureGoldWeight = result.PureGoldWeight,
            MakingType = input.MakingType,
            MakingValue = input.MakingValue,
            RateUsed24K = rate24K,
            GoldValue = result.GoldValue,
            MakingAmount = result.MakingAmount,
            StoneCharge = 0m,
            TaxableAmount = result.TaxableAmount,
            Cgst = result.GoldGST / 2m + result.MakingGST / 2m,
            Sgst = result.GoldGST / 2m + result.MakingGST / 2m,
            LineTotal = result.LineTotal
        };
    }

    public void RecalculateTotals()
    {
        var lineResults = LineItems.Select(li => new GoldPriceCalculator.BillLineResult(
            NetWeight: li.NetWeight,
            PurityFactor: 0,
            PureGoldWeight: li.PureGoldWeight,
            RatePerGram: li.RateUsed24K / 10m,
            GoldValue: li.GoldValue,
            MakingAmount: li.MakingAmount,
            WastageWeight: li.WastageWeight,
            WastageValue: li.WastagePercent > 0
                ? li.WastageWeight * (li.Purity switch { "22K" => 22m / 24m, "18K" => 18m / 24m, _ => 1m }) * (li.RateUsed24K / 10m)
                : 0m,
            TaxableAmount: li.TaxableAmount,
            GoldGST: li.Cgst + li.Sgst,
            MakingGST: 0m,
            StoneGST: 0m,
            LineTotal: li.LineTotal)).ToList();

        var totalInput = new GoldPriceCalculator.BillTotalInput(
            LineItems: lineResults,
            DiscountAmount: DiscountAmount,
            ExchangeValue: ExchangeValue,
            IsInterState: false);

        var totals = _calculator.CalculateTotal(totalInput, AmountPaid);

        GoldValueTotal = LineItems.Sum(l => l.GoldValue);
        MakingAmountTotal = LineItems.Sum(l => l.MakingAmount);
        WastageAmountTotal = LineItems.Sum(l => l.WastageWeight * (l.RateUsed24K / 10m)
            * (l.Purity switch { "22K" => 22m / 24m, "18K" => 18m / 24m, _ => 1m }));
        StoneChargeTotal = LineItems.Sum(l => l.StoneCharge);
        SubTotal = totals.SubTotal;
        TaxableAmount = totals.TaxableAmount;
        Cgst = totals.CGST;
        Sgst = totals.SGST;
        Igst = totals.IGST;
        RoundOff = totals.RoundOff;
        GrandTotal = totals.GrandTotal;
        BalanceDue = totals.BalanceDue;
        PaymentStatus = BalanceDue <= 0m ? "Paid" : "Partial";

        SaveBillCommand.NotifyCanExecuteChanged();
    }

    private bool ValidateForSave(out string error)
    {
        if (SelectedCustomer is null) { error = "Please select a customer"; return false; }
        if (LineItems.Count == 0) { error = "Add at least one item to the bill"; return false; }
        if (AmountPaid < 0) { error = "Amount paid cannot be negative"; return false; }
        if (DiscountAmount < 0) { error = "Discount cannot be negative"; return false; }
        if (string.IsNullOrWhiteSpace(SelectedPaymentMode)) { error = "Select a payment mode"; return false; }
        error = string.Empty;
        return true;
    }

    private CreateBillRequest BuildCreateBillRequest()
        => new(
            CustomerId: SelectedCustomer!.CustomerId,
            Items: LineItems.Select(l => new AddBillItemRequest(l.ItemId)).ToList(),
            DiscountAmount: DiscountAmount,
            ExchangeValue: ExchangeValue,
            PaymentMode: SelectedPaymentMode,
            AmountPaid: AmountPaid,
            UserId: AppState.CurrentUserId,
            BranchId: AppState.CurrentBranchId);

    private void ResetBill()
    {
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        CustomerSuggestions.Clear();
        IsCustomerSearchOpen = false;
        BarcodeInput = string.Empty;
        ScanStatusMessage = string.Empty;
        LineItems.Clear();
        SelectedLineItem = null;
        DiscountAmount = 0;
        ExchangeValue = 0;
        AmountPaid = 0;
        SelectedPaymentMode = "Cash";
        GoldValueTotal = 0; MakingAmountTotal = 0; WastageAmountTotal = 0;
        StoneChargeTotal = 0; SubTotal = 0; TaxableAmount = 0;
        Cgst = 0; Sgst = 0; Igst = 0; RoundOff = 0;
        GrandTotal = 0; BalanceDue = 0; PaymentStatus = "Pending";
        SavedBill = null;
        IsBillSaved = false;
        HasError = false;
        StatusMessage = string.Empty;
        RefreshRateDisplay();
        SaveBillCommand.NotifyCanExecuteChanged();
        PrintBillCommand.NotifyCanExecuteChanged();
    }
}
