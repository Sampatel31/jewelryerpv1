using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Services;

/// <summary>
/// Specialized query service for inventory reports and stock analysis.
/// </summary>
public class InventoryQueryService
{
    private readonly GoldDbContext _context;

    public InventoryQueryService(GoldDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public record StockValuationItem(
        int ItemId,
        string TagNo,
        string CategoryName,
        string Purity,
        decimal NetWeight,
        decimal PureGoldWeight,
        decimal CostPrice,
        decimal CurrentMarketValue,
        decimal UnrealizedGain);

    public record StockAgeingItem(
        int ItemId,
        string TagNo,
        string CategoryName,
        DateOnly PurchaseDate,
        int DaysInStock,
        string AgeingBucket);

    public record StockHealthSummary(
        int TotalItems,
        decimal TotalNetWeight,
        decimal TotalPureGoldWeight,
        decimal TotalCostValue,
        decimal TotalMarketValue,
        int ItemsUnder30Days,
        int Items31To60Days,
        int Items61To90Days,
        int ItemsOver90Days);

    /// <summary>Returns per-item market valuation compared to cost price.</summary>
    public async Task<IEnumerable<StockValuationItem>> GetStockValuationAsync(int branchId, decimal currentRate24K, CancellationToken cancellationToken = default)
    {
        var ratePerGram = currentRate24K / 10m;
        var items = await _context.Items
            .Include(i => i.Category)
            .Where(i => i.BranchId == branchId && i.Status == "InStock")
            .ToListAsync(cancellationToken);

        return items.Select(i =>
        {
            var marketValue = i.PureGoldWeight * ratePerGram;
            return new StockValuationItem(
                i.ItemId,
                i.TagNo,
                i.Category.Name,
                i.Purity,
                i.NetWeight,
                i.PureGoldWeight,
                i.CostPrice,
                marketValue,
                marketValue - i.CostPrice);
        });
    }

    /// <summary>Returns stock items with ageing buckets (0-30, 31-60, 61-90, 90+ days).</summary>
    public async Task<IEnumerable<StockAgeingItem>> GetStockAgeingAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var items = await _context.Items
            .Include(i => i.Category)
            .Where(i => i.BranchId == branchId && i.Status == "InStock")
            .ToListAsync(cancellationToken);

        return items.Select(i =>
        {
            var days = today.DayNumber - i.PurchaseDate.DayNumber;
            var bucket = days switch
            {
                <= 30 => "0-30 days",
                <= 60 => "31-60 days",
                <= 90 => "61-90 days",
                _     => "90+ days"
            };
            return new StockAgeingItem(i.ItemId, i.TagNo, i.Category.Name, i.PurchaseDate, days, bucket);
        });
    }

    /// <summary>Returns items in stock longer than daysThreshold days.</summary>
    public async Task<IEnumerable<StockAgeingItem>> GetSlowMovingItemsAsync(int branchId, int daysThreshold, CancellationToken cancellationToken = default)
    {
        var ageing = await GetStockAgeingAsync(branchId, cancellationToken);
        return ageing.Where(a => a.DaysInStock > daysThreshold);
    }

    /// <summary>Returns aggregate stock health metrics for a branch.</summary>
    public async Task<StockHealthSummary> GetStockHealthAsync(int branchId, decimal currentRate24K, CancellationToken cancellationToken = default)
    {
        var ratePerGram = currentRate24K / 10m;
        var items = await _context.Items
            .Where(i => i.BranchId == branchId && i.Status == "InStock")
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);

        return new StockHealthSummary(
            TotalItems: items.Count,
            TotalNetWeight: items.Sum(i => i.NetWeight),
            TotalPureGoldWeight: items.Sum(i => i.PureGoldWeight),
            TotalCostValue: items.Sum(i => i.CostPrice),
            TotalMarketValue: items.Sum(i => i.PureGoldWeight) * ratePerGram,
            ItemsUnder30Days: items.Count(i => today.DayNumber - i.PurchaseDate.DayNumber <= 30),
            Items31To60Days: items.Count(i => { var d = today.DayNumber - i.PurchaseDate.DayNumber; return d is > 30 and <= 60; }),
            Items61To90Days: items.Count(i => { var d = today.DayNumber - i.PurchaseDate.DayNumber; return d is > 60 and <= 90; }),
            ItemsOver90Days: items.Count(i => today.DayNumber - i.PurchaseDate.DayNumber > 90));
    }
}
