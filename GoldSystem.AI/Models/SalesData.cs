using Microsoft.ML.Data;

namespace GoldSystem.AI.Models;

/// <summary>
/// Input row for sales forecasting model — one record per day.
/// </summary>
public class DailySales
{
    [LoadColumn(0)]
    public float DayIndex { get; set; }

    [LoadColumn(1)]
    public float DailyRevenue { get; set; }
}

/// <summary>
/// SSA forecast output — arrays of length equal to the configured horizon.
/// </summary>
public class SalesForecast
{
    [ColumnName("Forecast")]
    public float[] Forecast { get; set; } = [];

    [ColumnName("LowerBound")]
    public float[] LowerBound { get; set; } = [];

    [ColumnName("UpperBound")]
    public float[] UpperBound { get; set; } = [];
}

/// <summary>Aggregated daily revenue used internally during training.</summary>
public record DailySalesAggregate(DateTime Date, decimal TotalRevenue, int BillCount);

/// <summary>Single-day revenue forecast result returned to callers.</summary>
public record ForecastResult(DateTime Day, decimal ForecastedRevenue, decimal LowerBound, decimal UpperBound);
