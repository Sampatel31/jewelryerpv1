using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Models;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Services;
using GoldSystem.WPF.Services;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Phase 11 – Customer Management ViewModel with three tabs:
/// Tab 1: Customer list with CRUD, search, and sort.
/// Tab 2: Customer ledger with bill history and outstanding balance tracking.
/// Tab 3: Loyalty points with tiered system, accrual, and redemption.
/// </summary>
public sealed partial class CustomerViewModel : BaseViewModel
{
    private readonly IUnitOfWork _uow;
    private readonly StockTransferService _transferService;
    private readonly ILogger<CustomerViewModel> _logger;

    // ── Loading / status ────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    // ── Tab selection ───────────────────────────────────────────────────────
    [ObservableProperty] private int _selectedTabIndex;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 1 – Customer CRUD
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<Customer> _customers = [];
    [ObservableProperty] private ObservableCollection<Customer> _filteredCustomers = [];
    [ObservableProperty] private Customer? _selectedCustomer;

    // Search / sort
    [ObservableProperty] private string _customerSearchText = string.Empty;
    [ObservableProperty] private string _selectedSortField = "Name";

    public static IReadOnlyList<string> SortFields { get; } =
        ["Name", "Phone", "TotalPurchased", "LoyaltyPoints", "CreatedAt"];

    // Edit form
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editEmail = string.Empty;
    [ObservableProperty] private string _editAddress = string.Empty;
    [ObservableProperty] private string _editGstin = string.Empty;
    [ObservableProperty] private decimal _editCreditLimit;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isNewCustomer;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 2 – Customer Ledger
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private Customer? _ledgerCustomer;
    [ObservableProperty] private ObservableCollection<CustomerLedgerEntry> _ledgerEntries = [];
    [ObservableProperty] private DateTime _ledgerFromDate = DateTime.Today.AddMonths(-3);
    [ObservableProperty] private DateTime _ledgerToDate = DateTime.Today;
    [ObservableProperty] private decimal _totalBilled;
    [ObservableProperty] private decimal _totalPaid;
    [ObservableProperty] private decimal _totalOutstanding;
    [ObservableProperty] private int _billCount;
    [ObservableProperty] private string _ledgerSearchText = string.Empty;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 3 – Loyalty Points
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<LoyaltyInfo> _loyaltyLeaderboard = [];
    [ObservableProperty] private LoyaltyInfo? _selectedLoyaltyCustomer;
    [ObservableProperty] private int _pointsToRedeem;
    [ObservableProperty] private string _loyaltyStatusMessage = string.Empty;
    [ObservableProperty] private bool _isRedeeming;

    public static string LoyaltyTierThresholds =>
        "🥈 Silver: ₹0 – ₹99,999\n🥇 Gold: ₹1,00,000 – ₹4,99,999\n💎 Platinum: ₹5,00,000+";

    public CustomerViewModel(
        NavigationService navigation,
        AppState appState,
        IUnitOfWork uow,
        StockTransferService transferService,
        ILogger<CustomerViewModel> logger)
        : base(navigation, appState)
    {
        _uow = uow;
        _transferService = transferService;
        _logger = logger;
    }

    public override async Task OnNavigatedToAsync()
    {
        await LoadAsync();
    }

    // ─── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        StatusMessage = "Loading customers…";

        try
        {
            await LoadCustomersAsync();
            await LoadLoyaltyLeaderboardAsync();
            StatusMessage = $"Loaded {Customers.Count} customers";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Load failed: {ex.Message}";
            _logger.LogError(ex, "Failed to load customers");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Tab 1: CRUD ───────────────────────────────────────────────────────────

    [RelayCommand]
    private void ApplyCustomerFilter()
    {
        var query = Customers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            var q = CustomerSearchText.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Phone.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (c.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        query = SelectedSortField switch
        {
            "Phone" => query.OrderBy(c => c.Phone),
            "TotalPurchased" => query.OrderByDescending(c => c.TotalPurchased),
            "LoyaltyPoints" => query.OrderByDescending(c => c.LoyaltyPoints),
            "CreatedAt" => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name)
        };

        FilteredCustomers = new ObservableCollection<Customer>(query.ToList());
    }

    [RelayCommand]
    private void NewCustomer()
    {
        IsNewCustomer = true;
        IsEditMode = true;
        SelectedCustomer = null;
        ClearEditForm();
    }

    [RelayCommand]
    private void EditCustomer(Customer? customer)
    {
        if (customer is null) return;
        SelectedCustomer = customer;
        IsNewCustomer = false;
        IsEditMode = true;
        PopulateEditForm(customer);
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (!ValidateEditForm(out var error))
        {
            HasError = true;
            StatusMessage = error;
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            if (IsNewCustomer)
            {
                var newCustomer = new Customer
                {
                    Name = EditName.Trim(),
                    Phone = EditPhone.Trim(),
                    Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim(),
                    Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim(),
                    GSTIN = string.IsNullOrWhiteSpace(EditGstin) ? null : EditGstin.Trim(),
                    CreditLimit = EditCreditLimit,
                    BranchId = AppState.CurrentBranchId,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Customers.AddAsync(newCustomer);
                await _uow.SaveChangesAsync();
                StatusMessage = $"✓ Customer '{newCustomer.Name}' added";
            }
            else if (SelectedCustomer is not null)
            {
                SelectedCustomer.Name = EditName.Trim();
                SelectedCustomer.Phone = EditPhone.Trim();
                SelectedCustomer.Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim();
                SelectedCustomer.Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim();
                SelectedCustomer.GSTIN = string.IsNullOrWhiteSpace(EditGstin) ? null : EditGstin.Trim();
                SelectedCustomer.CreditLimit = EditCreditLimit;
                await _uow.Customers.UpdateAsync(SelectedCustomer);
                await _uow.SaveChangesAsync();
                StatusMessage = $"✓ Customer '{SelectedCustomer.Name}' updated";
            }

            IsEditMode = false;
            IsNewCustomer = false;
            ClearEditForm();
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"✗ Save failed: {ex.Message}";
            _logger.LogError(ex, "Customer save failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer? customer)
    {
        if (customer is null) return;

        try
        {
            await _uow.Customers.DeleteAsync(customer);
            await _uow.SaveChangesAsync();
            Customers.Remove(customer);
            FilteredCustomers.Remove(customer);
            StatusMessage = $"✓ Customer '{customer.Name}' deleted";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"✗ Delete failed: {ex.Message}";
            _logger.LogError(ex, "Customer delete failed");
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditMode = false;
        IsNewCustomer = false;
        ClearEditForm();
        HasError = false;
        StatusMessage = string.Empty;
    }

    // ── Tab 2: Ledger ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadLedgerAsync()
    {
        if (LedgerCustomer is null) return;

        IsLoading = true;
        HasError = false;

        try
        {
            var fromDate = DateOnly.FromDateTime(LedgerFromDate);
            var toDate = DateOnly.FromDateTime(LedgerToDate);

            var bills = await _uow.Customers.GetCustomerLedgerAsync(
                LedgerCustomer.CustomerId, fromDate, toDate);

            var entries = bills.Select(b => new CustomerLedgerEntry(
                BillId: b.BillId,
                BillNo: b.BillNo,
                BillDate: b.BillDate,
                GrandTotal: b.GrandTotal,
                AmountPaid: b.AmountPaid,
                BalanceDue: b.BalanceDue,
                Status: b.Status,
                PaymentMode: b.PaymentMode,
                ItemCount: 0)).ToList();

            LedgerEntries = new ObservableCollection<CustomerLedgerEntry>(entries);
            BillCount = entries.Count;
            TotalBilled = entries.Sum(e => e.GrandTotal);
            TotalPaid = entries.Sum(e => e.AmountPaid);
            TotalOutstanding = entries.Sum(e => e.BalanceDue);

            StatusMessage = $"Ledger: {BillCount} bills, ₹{TotalBilled:N0} total, ₹{TotalOutstanding:N0} outstanding";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"✗ Ledger load failed: {ex.Message}";
            _logger.LogError(ex, "Customer ledger load failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectLedgerCustomer(Customer? customer)
    {
        if (customer is null) return;
        LedgerCustomer = customer;
        SelectedTabIndex = 1; // Switch to ledger tab
        _ = LoadLedgerAsync();
    }

    // ── Tab 3: Loyalty ────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRedeem))]
    private async Task RedeemPointsAsync()
    {
        if (SelectedLoyaltyCustomer is null || PointsToRedeem <= 0) return;

        IsRedeeming = true;
        LoyaltyStatusMessage = "Processing redemption…";

        try
        {
            var request = new LoyaltyRedemptionRequest(
                CustomerId: SelectedLoyaltyCustomer.CustomerId,
                PointsToRedeem: PointsToRedeem,
                UserId: AppState.CurrentUserId);

            var result = await _transferService.RedeemPointsAsync(request);

            LoyaltyStatusMessage = result.Success
                ? $"✓ {result.Message}"
                : $"✗ {result.Message}";

            if (result.Success)
            {
                PointsToRedeem = 0;
                await LoadLoyaltyLeaderboardAsync();
            }
        }
        catch (Exception ex)
        {
            LoyaltyStatusMessage = $"✗ Redemption failed: {ex.Message}";
            _logger.LogError(ex, "Loyalty redemption failed");
        }
        finally
        {
            IsRedeeming = false;
        }
    }

    private bool CanRedeem() =>
        SelectedLoyaltyCustomer is not null &&
        PointsToRedeem > 0 &&
        PointsToRedeem <= (SelectedLoyaltyCustomer?.TotalPoints ?? 0) &&
        !IsRedeeming;

    // ─── Property-change handlers ─────────────────────────────────────────────

    partial void OnCustomerSearchTextChanged(string _) => ApplyCustomerFilter();
    partial void OnSelectedSortFieldChanged(string _) => ApplyCustomerFilter();

    partial void OnSelectedLoyaltyCustomerChanged(LoyaltyInfo? _)
        => RedeemPointsCommand.NotifyCanExecuteChanged();
    partial void OnPointsToRedeemChanged(int _)
        => RedeemPointsCommand.NotifyCanExecuteChanged();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task LoadCustomersAsync()
    {
        var all = await _uow.Customers.GetAllAsync();
        var branchCustomers = all.Where(c => c.BranchId == AppState.CurrentBranchId).ToList();
        Customers = new ObservableCollection<Customer>(branchCustomers);
        ApplyCustomerFilter();
    }

    private async Task LoadLoyaltyLeaderboardAsync()
    {
        var top = await _uow.Customers.GetTopCustomersByVolumeAsync(50, AppState.CurrentBranchId);

        var leaderboard = top.Select(c =>
        {
            var tier = StockTransferService.GetTier(c.TotalPurchased);
            return new LoyaltyInfo(
                CustomerId: c.CustomerId,
                CustomerName: c.Name,
                TotalPoints: c.LoyaltyPoints,
                Tier: tier,
                TotalPurchased: c.TotalPurchased,
                PointsValueInRupees: (c.LoyaltyPoints / 100m) * 50m,
                PointsToNextTier: PointsToNextTier(tier, c.TotalPurchased),
                NextTierName: NextTierName(tier));
        }).ToList();

        LoyaltyLeaderboard = new ObservableCollection<LoyaltyInfo>(leaderboard);
    }

    private static int PointsToNextTier(LoyaltyTier tier, decimal totalPurchased) => tier switch
    {
        LoyaltyTier.Silver => (int)((100_000m - totalPurchased) / 1000m),
        LoyaltyTier.Gold => (int)((500_000m - totalPurchased) / 1000m),
        _ => 0
    };

    private static string NextTierName(LoyaltyTier tier) => tier switch
    {
        LoyaltyTier.Silver => "Gold",
        LoyaltyTier.Gold => "Platinum",
        _ => "–"
    };

    private void ClearEditForm()
    {
        EditName = string.Empty;
        EditPhone = string.Empty;
        EditEmail = string.Empty;
        EditAddress = string.Empty;
        EditGstin = string.Empty;
        EditCreditLimit = 0m;
    }

    private void PopulateEditForm(Customer c)
    {
        EditName = c.Name;
        EditPhone = c.Phone;
        EditEmail = c.Email ?? string.Empty;
        EditAddress = c.Address ?? string.Empty;
        EditGstin = c.GSTIN ?? string.Empty;
        EditCreditLimit = c.CreditLimit;
    }

    private bool ValidateEditForm(out string error)
    {
        if (string.IsNullOrWhiteSpace(EditName)) { error = "Customer name is required."; return false; }
        if (string.IsNullOrWhiteSpace(EditPhone)) { error = "Phone number is required."; return false; }
        if (EditPhone.Trim().Length < 10) { error = "Phone number must be at least 10 digits."; return false; }
        if (EditCreditLimit < 0) { error = "Credit limit cannot be negative."; return false; }
        error = string.Empty;
        return true;
    }
}
