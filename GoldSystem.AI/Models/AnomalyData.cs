using Microsoft.ML.Data;

namespace GoldSystem.AI.Models;

/// <summary>Input row for the IID spike-detection pipeline.</summary>
public class BillAmountInput
{
    [LoadColumn(0)]
    public float Amount { get; set; }
}

/// <summary>
/// IID spike-detection output.
/// Prediction[0] = 1 (spike) / 0 (normal), [1] = raw score, [2] = p-value.
/// </summary>
public class BillPrediction
{
    [VectorType(3)]
    [ColumnName("Prediction")]
    public double[] Prediction { get; set; } = [];
}

/// <summary>Anomaly alert raised for a single bill.</summary>
public class AnomalyAlert
{
    public int BillId { get; set; }
    public string BillNo { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string AlertReason { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}
