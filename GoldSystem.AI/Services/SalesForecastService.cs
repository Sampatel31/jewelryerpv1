using GoldSystem.AI.Models;
using GoldSystem.Data;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace GoldSystem.AI.Services;

public interface ISalesForecastService
{
    Task TrainIfDataSufficientAsync();
    Task<List<ForecastResult>> PredictNext7DaysAsync();
    Task<bool> HasSufficientDataAsync();
}

/// <summary>
/// Forecasts next 7 days of sales revenue using ML.NET Singular Spectrum Analysis (SSA).
/// Models are trained on 90 days of history and persisted to disk.
/// Falls back gracefully when insufficient data is available.
/// </summary>
public class SalesForecastService : ISalesForecastService
{
    private const string MODEL_PATH = "Models/sales_forecast.zip";

    private readonly IUnitOfWork _uow;
    private readonly ILogger<SalesForecastService> _logger;
    private ITransformer? _model;
    private MLContext? _mlContext;

    private readonly int MIN_DAYS = 30;
    private readonly int HISTORY_DAYS = 90;

    public SalesForecastService(IUnitOfWork uow, ILogger<SalesForecastService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task TrainIfDataSufficientAsync()
    {
        try
        {
            var hasData = await HasSufficientDataAsync();
            if (!hasData)
            {
                _logger.LogInformation("Insufficient sales data for forecasting. Need {Min} days.", MIN_DAYS);
                return;
            }

            _logger.LogInformation("Training sales forecast model...");

            var history = await GetDailySalesHistoryAsync(HISTORY_DAYS);
            if (!history.Any()) return;

            _mlContext = new MLContext(seed: 42);

            var salesData = history
                .Select((s, i) => new DailySales
                {
                    DayIndex = i,
                    DailyRevenue = (float)s.TotalRevenue
                })
                .ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(salesData);

            // Singular Spectrum Analysis for time-series forecasting.
            var pipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: "Forecast",
                inputColumnName: "DailyRevenue",
                windowSize: 7,
                seriesLength: 30,
                trainSize: history.Count,
                horizon: 7,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: "LowerBound",
                confidenceUpperBoundColumn: "UpperBound");

            _model = pipeline.Fit(dataView);

            Directory.CreateDirectory(Path.GetDirectoryName(MODEL_PATH)!);
            _mlContext.Model.Save(_model, dataView.Schema, MODEL_PATH);
            _logger.LogInformation("Sales forecast model trained and saved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training sales forecast model");
        }
    }

    public async Task<List<ForecastResult>> PredictNext7DaysAsync()
    {
        try
        {
            if (_model == null)
                await LoadOrTrainModelAsync();

            if (_model == null)
                return [];

            // PredictionFunctionExtensions.CreateTimeSeriesEngine accepts ITransformer + IHostEnvironment.
            // MLContext implements IHostEnvironment, so this is valid.
            var engine = _model.CreateTimeSeriesEngine<DailySales, SalesForecast>(_mlContext!);
            var prediction = engine.Predict();

            var results = new List<ForecastResult>();
            for (int i = 0; i < prediction.Forecast.Length; i++)
            {
                results.Add(new ForecastResult(
                    Day: DateTime.Today.AddDays(i + 1),
                    ForecastedRevenue: (decimal)prediction.Forecast[i],
                    LowerBound: prediction.LowerBound.Length > i ? (decimal)prediction.LowerBound[i] : 0m,
                    UpperBound: prediction.UpperBound.Length > i ? (decimal)prediction.UpperBound[i] : 0m));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting sales forecast");
            return [];
        }
    }

    public async Task<bool> HasSufficientDataAsync()
    {
        var toDate = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-MIN_DAYS));

        // Query across all branches (no branchId filter for global forecast).
        var sales = await _uow.Bills.FindAsync(b => b.BillDate >= fromDate && b.BillDate <= toDate);

        // Need at least MIN_DAYS distinct dates with sales.
        return sales.Select(b => b.BillDate).Distinct().Count() >= MIN_DAYS;
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task<List<DailySalesAggregate>> GetDailySalesHistoryAsync(int days)
    {
        var toDate = DateOnly.FromDateTime(DateTime.Today);
        var fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-days));

        var bills = await _uow.Bills.FindAsync(b => b.BillDate >= fromDate && b.BillDate <= toDate);

        return bills
            .GroupBy(b => b.BillDate)
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesAggregate(
                Date: g.Key.ToDateTime(TimeOnly.MinValue),
                TotalRevenue: g.Sum(b => b.GrandTotal),
                BillCount: g.Count()))
            .ToList();
    }

    private async Task LoadOrTrainModelAsync()
    {
        if (File.Exists(MODEL_PATH))
        {
            try
            {
                _mlContext = new MLContext(seed: 42);
                _model = _mlContext.Model.Load(MODEL_PATH, out _);
                _logger.LogInformation("Sales forecast model loaded from disk.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales forecast model. Retraining...");
                await TrainIfDataSufficientAsync();
            }
        }
        else
        {
            await TrainIfDataSufficientAsync();
        }
    }
}
