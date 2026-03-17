using CommunityToolkit.Mvvm.ComponentModel;

namespace GoldSystem.WPF.ViewModels;

/// <summary>
/// Observable view-model for a single bill line item shown in the billing DataGrid.
/// All monetary properties are computed from raw inputs via GoldPriceCalculator.
/// </summary>
public sealed partial class BillLineItemViewModel : ObservableObject
{
    // ── Identity ────────────────────────────────────────────────────────────
    [ObservableProperty] private int _itemId;
    [ObservableProperty] private string _tagNo = string.Empty;
    [ObservableProperty] private string _huid = string.Empty;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private string _purity = string.Empty;
    [ObservableProperty] private string _categoryName = string.Empty;

    // ── Weight ──────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _grossWeight;
    [ObservableProperty] private decimal _stoneWeight;
    [ObservableProperty] private decimal _netWeight;
    [ObservableProperty] private decimal _wastagePercent;
    [ObservableProperty] private decimal _wastageWeight;
    [ObservableProperty] private decimal _billableWeight;
    [ObservableProperty] private decimal _pureGoldWeight;

    // ── Making ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _makingType = string.Empty;
    [ObservableProperty] private decimal _makingValue;

    // ── Pricing ─────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _rateUsed24K;
    [ObservableProperty] private decimal _goldValue;
    [ObservableProperty] private decimal _makingAmount;
    [ObservableProperty] private decimal _stoneCharge;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _cgst;
    [ObservableProperty] private decimal _sgst;
    [ObservableProperty] private decimal _lineTotal;

    /// <summary>Human-readable making description, e.g. "12% of Gold" or "₹350/g".</summary>
    public string MakingDescription => MakingType switch
    {
        "PERCENT" => $"{MakingValue:0.##}% of Gold",
        "PER_GRAM" => $"₹{MakingValue:N0}/g",
        "FIXED" => $"₹{MakingValue:N0} Fixed",
        _ => MakingType
    };

    /// <summary>Purity + tag display, e.g. "22K | TAG-001".</summary>
    public string PurityTagDisplay => $"{Purity} | {TagNo}";
}
