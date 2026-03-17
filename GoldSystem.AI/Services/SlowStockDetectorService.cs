using GoldSystem.AI.Models;
using GoldSystem.Data;
using Microsoft.Extensions.Logging;

namespace GoldSystem.AI.Services;

public interface ISlowStockDetectorService
{
    Task<List<SlowStockAlert>> DetectSlowStockAsync(int branchId);
}

/// <summary>
/// Detects inventory items that have been in stock longer than 1.5× the
/// category average sell-through time, using pure LINQ analysis.
/// </summary>
public class SlowStockDetectorService : ISlowStockDetectorService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<SlowStockDetectorService> _logger;

    // Items that take more than 1.5× the category average days to sell are flagged.
    // This means they are taking 50% longer than normal to move, indicating slow stock.
    private const double THRESHOLD_MULTIPLIER = 1.5;

    public SlowStockDetectorService(IUnitOfWork uow, ILogger<SlowStockDetectorService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<SlowStockAlert>> DetectSlowStockAsync(int branchId)
    {
        try
        {
            // Compute average days-in-stock per category from already-sold items.
            var soldItems = (await _uow.Items.FindAsync(i =>
                    i.Status == "Sold" && i.SoldBillId.HasValue && i.BranchId == branchId))
                .ToList();

            // Build category → average days-in-stock from historical sell data.
            // PurchaseDate is DateOnly so convert before subtraction.
            var categoryAvgDays = soldItems
                .GroupBy(i => i.CategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(i =>
                        (DateTime.Today - i.PurchaseDate.ToDateTime(TimeOnly.MinValue)).TotalDays));

            // Evaluate all currently in-stock items.
            var inStockItems = (await _uow.Items.FindAsync(i =>
                    i.Status == "InStock" && i.BranchId == branchId))
                .ToList();

            var slowItems = inStockItems
                .Select(i => new
                {
                    Item = i,
                    DaysInStock = (DateTime.Today - i.PurchaseDate.ToDateTime(TimeOnly.MinValue)).TotalDays,
                    AvgDays = categoryAvgDays.GetValueOrDefault(i.CategoryId, 30.0)
                })
                .Where(x => x.DaysInStock > x.AvgDays * THRESHOLD_MULTIPLIER)
                .OrderByDescending(x => x.DaysInStock)
                .Select(x => new SlowStockAlert
                {
                    Item = x.Item,
                    DaysInStock = x.DaysInStock,
                    CategoryAverageDays = x.AvgDays,
                    ExcessDays = x.DaysInStock - x.AvgDays,
                    AlertedAt = DateTime.UtcNow
                })
                .ToList();

            if (slowItems.Any())
                _logger.LogWarning(
                    "Detected {Count} slow-moving items in branch {BranchId}",
                    slowItems.Count, branchId);

            return slowItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting slow stock");
            return [];
        }
    }
}
