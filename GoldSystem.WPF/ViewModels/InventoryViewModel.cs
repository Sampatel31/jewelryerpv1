using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoldSystem.Core.Models;
using GoldSystem.Core.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Services;
using GoldSystem.WPF.Services;
using Microsoft.Extensions.Logging;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Phase 11 – Inventory Management ViewModel with two tabs:
/// Tab 1: Item list with search/filter/KPI cards and CRUD operations.
/// Tab 2: Stock transfers between branches with validation and audit trail.
/// </summary>
public sealed partial class InventoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _uow;
    private readonly StockTransferService _transferService;
    private readonly ILogger<InventoryViewModel> _logger;

    // ── Loading / status ────────────────────────────────────────────────────
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasError;

    // ── Tab selection ───────────────────────────────────────────────────────
    [ObservableProperty] private int _selectedTabIndex;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 1 – Item list
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<InventoryItemDto> _allItems = [];
    [ObservableProperty] private ObservableCollection<InventoryItemDto> _filteredItems = [];
    [ObservableProperty] private InventoryItemDto? _selectedItem;

    // Search / filter
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedStatusFilter = "All";

    public static IReadOnlyList<string> StatusFilters { get; } =
        ["All", "InStock", "Sold", "Reserved", "Transferred"];

    // KPI cards
    [ObservableProperty] private int _inStockCount;
    [ObservableProperty] private decimal _inStockValue;
    [ObservableProperty] private int _agingItemsCount;    // items in stock > 90 days
    [ObservableProperty] private decimal _soldThisMonth;

    // ══════════════════════════════════════════════════════════════════════════
    // TAB 2 – Stock Transfers
    // ══════════════════════════════════════════════════════════════════════════

    [ObservableProperty] private ObservableCollection<InventoryItemDto> _transferableItems = [];
    [ObservableProperty] private InventoryItemDto? _selectedTransferItem;
    [ObservableProperty] private ObservableCollection<BranchDto> _branches = [];
    [ObservableProperty] private BranchDto? _selectedDestinationBranch;
    [ObservableProperty] private string _transferRemarks = string.Empty;
    [ObservableProperty] private ObservableCollection<StockTransferDto> _transferHistory = [];
    [ObservableProperty] private bool _isTransferring;

    public InventoryViewModel(
        NavigationService navigation,
        AppState appState,
        IUnitOfWork uow,
        StockTransferService transferService,
        ILogger<InventoryViewModel> logger)
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
        StatusMessage = "Loading inventory…";

        try
        {
            await LoadItemsAsync();
            await LoadBranchesAsync();
            UpdateKpis();
            StatusMessage = $"Loaded {AllItems.Count} items";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Load failed: {ex.Message}";
            _logger.LogError(ex, "Failed to load inventory");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var query = AllItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim().ToLowerInvariant();
            query = query.Where(i =>
                (i.HUID?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                i.TagNo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Purity.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedStatusFilter != "All")
            query = query.Where(i => i.Status == SelectedStatusFilter);

        FilteredItems = new ObservableCollection<InventoryItemDto>(query.ToList());
        TransferableItems = new ObservableCollection<InventoryItemDto>(
            AllItems.Where(i => i.Status == "InStock" && i.BranchId == AppState.CurrentBranchId).ToList());
    }

    [RelayCommand(CanExecute = nameof(CanTransfer))]
    private async Task TransferItemAsync()
    {
        if (SelectedTransferItem is null || SelectedDestinationBranch is null) return;

        IsTransferring = true;
        HasError = false;
        StatusMessage = "Transferring item…";

        try
        {
            var request = new StockTransferRequest(
                ItemId: SelectedTransferItem.ItemId,
                FromBranchId: AppState.CurrentBranchId,
                ToBranchId: SelectedDestinationBranch.BranchId,
                UserId: AppState.CurrentUserId,
                Remarks: TransferRemarks.Trim());

            var result = await _transferService.TransferItemAsync(request);
            TransferHistory.Insert(0, result);

            StatusMessage = $"✓ Item '{result.TagNo}' transferred to {result.ToBranchName}";
            SelectedTransferItem = null;
            SelectedDestinationBranch = null;
            TransferRemarks = string.Empty;

            await LoadItemsAsync();
            UpdateKpis();
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"✗ Transfer failed: {ex.Message}";
            _logger.LogError(ex, "Stock transfer failed");
        }
        finally
        {
            IsTransferring = false;
        }
    }

    private bool CanTransfer() =>
        SelectedTransferItem is not null &&
        SelectedDestinationBranch is not null &&
        SelectedDestinationBranch.BranchId != AppState.CurrentBranchId &&
        !IsTransferring;

    // ─── Property-change handlers ─────────────────────────────────────────────

    partial void OnSearchTextChanged(string _) => ApplyFilter();
    partial void OnSelectedStatusFilterChanged(string _) => ApplyFilter();

    partial void OnSelectedTransferItemChanged(InventoryItemDto? _)
        => TransferItemCommand.NotifyCanExecuteChanged();
    partial void OnSelectedDestinationBranchChanged(BranchDto? _)
        => TransferItemCommand.NotifyCanExecuteChanged();

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task LoadItemsAsync()
    {
        var items = await _transferService.GetInventoryAsync(AppState.CurrentBranchId);
        AllItems = new ObservableCollection<InventoryItemDto>(items);
        ApplyFilter();
    }

    private async Task LoadBranchesAsync()
    {
        var activeBranches = await _uow.Branches.GetActiveBranchesAsync();
        Branches = new ObservableCollection<BranchDto>(
            activeBranches
                .Where(b => b.BranchId != AppState.CurrentBranchId)
                .Select(b => new BranchDto(b.BranchId, b.Name, b.Code)));
    }

    private void UpdateKpis()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        InStockCount = AllItems.Count(i => i.Status == "InStock");
        InStockValue = AllItems.Where(i => i.Status == "InStock").Sum(i => i.CostPrice);
        AgingItemsCount = AllItems.Count(i => i.Status == "InStock" && i.DaysInStock > 90);
        SoldThisMonth = 0m; // populated from billing data if available
    }
}

/// <summary>Minimal branch display DTO for the transfer destination picker.</summary>
public record BranchDto(int BranchId, string Name, string Code);
