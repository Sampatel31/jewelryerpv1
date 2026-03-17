using GoldSystem.AI.Models;
using GoldSystem.Data;
using MathNet.Numerics;
using Microsoft.Extensions.Logging;

namespace GoldSystem.AI.Services;

public interface IRateTrendAnalyzerService
{
    Task<RateTrendResult> AnalyzeTrendAsync(int branchId);
    Task<bool> HasSufficientDataAsync(int branchId);
}

/// <summary>
/// Analyses 22K gold rate history using ordinary-least-squares linear regression
/// (MathNet.Numerics) to determine whether the trend is UP, DOWN, or STABLE.
/// No ML.NET model is required — the analysis is purely numeric.
/// </summary>
public class RateTrendAnalyzerService : IRateTrendAnalyzerService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RateTrendAnalyzerService> _logger;
    private const int MIN_DAYS = 14;
    private const int ANALYSIS_DAYS = 30;

    // ±₹2/day slope indicates meaningful price movement; within this range is STABLE.
    private const double SLOPE_THRESHOLD = 2.0;

    public RateTrendAnalyzerService(IUnitOfWork uow, ILogger<RateTrendAnalyzerService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RateTrendResult> AnalyzeTrendAsync(int branchId)
    {
        try
        {
            var hasData = await HasSufficientDataAsync(branchId);
            if (!hasData)
            {
                _logger.LogInformation(
                    "Insufficient rate history for branch {BranchId}. Need {Min} days.", branchId, MIN_DAYS);
                return new RateTrendResult
                {
                    Direction = "INSUFFICIENT_DATA",
                    ChangeOver30Days = 0,
                    Slope = 0,
                    CurrentRate = 0
                };
            }

            var rates = (await _uow.GoldRates.GetRateHistoryAsync(branchId, ANALYSIS_DAYS))
                .OrderBy(r => r.RateDate)
                .ToList();

            if (!rates.Any())
                return new RateTrendResult { Direction = "NO_DATA" };

            var xValues = rates.Select((_, i) => (double)i).ToArray();
            var yValues = rates.Select(r => (double)r.Rate22K).ToArray();

            // OLS linear regression via MathNet.Numerics.
            var (_, slope) = Fit.Line(xValues, yValues);

            // Project the slope over 30 calendar days.
            var projectedChange = slope * ANALYSIS_DAYS;

            // Slope threshold: ±₹2/day is considered meaningful movement.
            var direction = slope > SLOPE_THRESHOLD ? "UP" : slope < -SLOPE_THRESHOLD ? "DOWN" : "STABLE";

            var currentRate = rates.Last().Rate22K;

            var result = new RateTrendResult
            {
                Direction = direction,
                ChangeOver30Days = (decimal)projectedChange,
                Slope = (decimal)slope,
                CurrentRate = currentRate
            };

            _logger.LogInformation(
                "Rate trend for branch {BranchId}: {Direction} (projected {Change:F2} over 30 days)",
                branchId, direction, projectedChange);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analysing rate trend for branch {BranchId}", branchId);
            return new RateTrendResult { Direction = "ERROR" };
        }
    }

    public async Task<bool> HasSufficientDataAsync(int branchId)
    {
        var rates = await _uow.GoldRates.GetRateHistoryAsync(branchId, MIN_DAYS);
        return rates.Count() >= MIN_DAYS;
    }
}
