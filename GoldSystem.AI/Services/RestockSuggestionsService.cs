using GoldSystem.AI.Models;
using GoldSystem.Data;
using Microsoft.Extensions.Logging;

namespace GoldSystem.AI.Services;

public interface IRestockSuggestionsService
{
    Task<List<RestockRecommendation>> GetRecommendationsAsync(int branchId);
}

/// <summary>
/// Generates category-level restock recommendations by comparing 90-day sales
/// velocity against current stock levels.  Velocity ≥ 2.0 (sold/stock) triggers
/// a restock suggestion.
/// </summary>
public class RestockSuggestionsService : IRestockSuggestionsService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RestockSuggestionsService> _logger;
    private const int ANALYSIS_DAYS = 90;

    // A category is flagged when sold/stock ratio ≥ 2.0: sold items are twice current stock.
    private const decimal VELOCITY_THRESHOLD = 2.0m;

    // Reorder 50% of the quantity sold in the analysis window as a conservative first order.
    private const decimal RESTOCK_FACTOR = 0.5m;

    public RestockSuggestionsService(IUnitOfWork uow, ILogger<RestockSuggestionsService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<RestockRecommendation>> GetRecommendationsAsync(int branchId)
    {
        try
        {
            var toDate = DateOnly.FromDateTime(DateTime.Today);
            var fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-ANALYSIS_DAYS));

            // Count sold items by category within the analysis window.
            // Items sold in a bill are reflected via the Item.SoldBillId FK.
            // We join through Bills to filter by date range and branch.
            var bills = await _uow.Bills.GetBillsByDateRangeAsync(fromDate, toDate, branchId);
            var billIds = bills.Select(b => b.BillId).ToHashSet();

            if (!billIds.Any())
                return [];

            // Items that were sold in those bills, grouped by CategoryId.
            var soldItems = (await _uow.Items.FindAsync(i =>
                    i.Status == "Sold" && i.SoldBillId.HasValue && i.BranchId == branchId))
                .Where(i => billIds.Contains(i.SoldBillId!.Value))
                .ToList();

            var soldByCategory = soldItems
                .GroupBy(i => i.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            // Current in-stock count by category.
            var inStockItems = (await _uow.Items.FindAsync(i =>
                    i.Status == "InStock" && i.BranchId == branchId))
                .ToList();

            var stockByCategory = inStockItems
                .GroupBy(i => i.CategoryId)
                .ToDictionary(g => g.Key, g => g.Count());

            var recommendations = new List<RestockRecommendation>();

            foreach (var category in await _uow.Categories.GetActiveCategoriesAsync())
            {
                var sold = soldByCategory.GetValueOrDefault(category.CategoryId, 0);
                if (sold == 0) continue;

                var stock = stockByCategory.GetValueOrDefault(category.CategoryId, 0);
                var velocity = stock > 0 ? (decimal)sold / stock : decimal.MaxValue;

                if (velocity >= VELOCITY_THRESHOLD)
                {
                    recommendations.Add(new RestockRecommendation
                    {
                        CategoryId = category.CategoryId,
                        CategoryName = category.Name,
                        CurrentStockCount = stock,
                        VelocityRatio = velocity == decimal.MaxValue ? 999m : velocity,
                        SuggestedOrderQty = (int)Math.Ceiling(sold * RESTOCK_FACTOR),
                        EstimatedCost = 0m   // Calculated externally once purchase rates are known.
                    });
                }
            }

            return recommendations
                .OrderByDescending(r => r.VelocityRatio)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating restock suggestions for branch {BranchId}", branchId);
            return [];
        }
    }
}
