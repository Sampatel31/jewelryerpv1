using GoldSystem.AI.Models;
using GoldSystem.Data;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace GoldSystem.AI.Services;

public interface IAnomalyDetectorService
{
    Task TrainIfDataSufficientAsync();
    Task<List<AnomalyAlert>> DetectAnomaliesAsync(int branchId);
    Task<bool> HasSufficientDataAsync(int branchId);
}

/// <summary>
/// Detects statistically anomalous bill amounts using ML.NET IID Spike Detection.
/// Alerts are informational only — billing is never blocked.
/// </summary>
public class AnomalyDetectorService : IAnomalyDetectorService
{
    private const string MODEL_PATH = "Models/anomaly_detector.zip";

    private readonly IUnitOfWork _uow;
    private readonly ILogger<AnomalyDetectorService> _logger;
    private ITransformer? _model;
    private MLContext? _mlContext;

    private const int MIN_BILLS = 50;

    public AnomalyDetectorService(IUnitOfWork uow, ILogger<AnomalyDetectorService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task TrainIfDataSufficientAsync()
    {
        try
        {
            // Train on all-branches data so the model reflects the full distribution.
            var totalBills = await _uow.Bills.CountAsync();
            if (totalBills < MIN_BILLS)
            {
                _logger.LogInformation(
                    "Insufficient bills for anomaly detection. Need {Min} bills.", MIN_BILLS);
                return;
            }

            _logger.LogInformation("Training anomaly detection model...");

            var bills = (await _uow.Bills.GetAllAsync()).ToList();

            _mlContext = new MLContext(seed: 1);

            var billData = bills
                .Select(b => new BillAmountInput { Amount = (float)b.GrandTotal })
                .ToList();

            var dataView = _mlContext.Data.LoadFromEnumerable(billData);

            // IID Spike Detection — stateless per-point detection, no temporal dependency.
            var pipeline = _mlContext.Transforms.DetectIidSpike(
                outputColumnName: "Prediction",
                inputColumnName: "Amount",
                confidence: 95.0,
                pvalueHistoryLength: 30);

            // DetectIidSpike is a stateless transform; fit against an empty schema.
            _model = pipeline.Fit(_mlContext.Data.LoadFromEnumerable(new List<BillAmountInput>()));

            Directory.CreateDirectory(Path.GetDirectoryName(MODEL_PATH)!);
            _mlContext.Model.Save(_model, dataView.Schema, MODEL_PATH);
            _logger.LogInformation("Anomaly detector model trained and saved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training anomaly detector");
        }
    }

    public async Task<List<AnomalyAlert>> DetectAnomaliesAsync(int branchId)
    {
        try
        {
            if (_model == null)
                await LoadOrTrainModelAsync();

            if (_model == null)
                return [];

            var bills = (await _uow.Bills.FindAsync(b => b.BranchId == branchId))
                .OrderBy(b => b.CreatedAt)
                .ToList();

            if (!bills.Any())
                return [];

            var billData = bills
                .Select(b => new BillAmountInput { Amount = (float)b.GrandTotal })
                .ToList();

            var dataView = _mlContext!.Data.LoadFromEnumerable(billData);
            var predictions = _mlContext.Data
                .CreateEnumerable<BillPrediction>(_model.Transform(dataView), reuseRowObject: false)
                .ToList();

            var anomalies = new List<AnomalyAlert>();

            for (int i = 0; i < bills.Count && i < predictions.Count; i++)
            {
                // Prediction[0] == 1 indicates a detected spike (anomaly).
                if (predictions[i].Prediction.Length > 0 && predictions[i].Prediction[0] == 1)
                {
                    anomalies.Add(new AnomalyAlert
                    {
                        BillId = bills[i].BillId,
                        BillNo = bills[i].BillNo,
                        GrandTotal = bills[i].GrandTotal,
                        AlertReason = "Unusual bill amount detected (statistically anomalous)",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }

            if (anomalies.Any())
                _logger.LogWarning(
                    "Detected {Count} anomalous bills in branch {BranchId}",
                    anomalies.Count, branchId);

            return anomalies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies for branch {BranchId}", branchId);
            return [];
        }
    }

    public async Task<bool> HasSufficientDataAsync(int branchId)
    {
        var billCount = await _uow.Bills.CountAsync(b => b.BranchId == branchId);
        return billCount >= MIN_BILLS;
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task LoadOrTrainModelAsync()
    {
        if (File.Exists(MODEL_PATH))
        {
            try
            {
                _mlContext = new MLContext(seed: 1);
                _model = _mlContext.Model.Load(MODEL_PATH, out _);
                _logger.LogInformation("Anomaly detector model loaded from disk.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading anomaly detector model. Retraining...");
                await TrainIfDataSufficientAsync();
            }
        }
        else
        {
            await TrainIfDataSufficientAsync();
        }
    }
}
