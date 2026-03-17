using GoldSystem.Data.Entities;

namespace GoldSystem.AI.Models;

/// <summary>Alert for a slow-moving inventory item.</summary>
public class SlowStockAlert
{
    public Item Item { get; set; } = null!;
    public double DaysInStock { get; set; }
    public double CategoryAverageDays { get; set; }
    public double ExcessDays { get; set; }
    public DateTime AlertedAt { get; set; }
}

/// <summary>Category-level restock recommendation based on velocity analysis.</summary>
public class RestockRecommendation
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentStockCount { get; set; }
    public decimal VelocityRatio { get; set; }
    public int SuggestedOrderQty { get; set; }
    public decimal EstimatedCost { get; set; }
}
