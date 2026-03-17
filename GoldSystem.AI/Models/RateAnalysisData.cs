using Microsoft.ML.Data;

namespace GoldSystem.AI.Models;

/// <summary>Single rate data point used for regression analysis.</summary>
public class RateDataPoint
{
    [LoadColumn(0)]
    public float DayIndex { get; set; }

    [LoadColumn(1)]
    public float Rate22K { get; set; }
}

/// <summary>Result of the 30-day rate trend analysis.</summary>
public class RateTrendResult
{
    /// <summary>"UP", "DOWN", "STABLE", "INSUFFICIENT_DATA", "NO_DATA", or "ERROR".</summary>
    public string Direction { get; set; } = string.Empty;

    public decimal ChangeOver30Days { get; set; }

    public decimal Slope { get; set; }

    public decimal CurrentRate { get; set; }
}
