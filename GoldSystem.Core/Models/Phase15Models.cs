namespace GoldSystem.Core.Models;

// ─── Custom Exceptions ────────────────────────────────────────────────────────

/// <summary>Thrown when user-supplied input fails a business validation rule.</summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>Thrown when an operation violates a business rule.</summary>
public class BusinessLogicException : Exception
{
    public BusinessLogicException(string message) : base(message) { }
}

/// <summary>Wraps a data-layer exception with a user-friendly message.</summary>
public class DataException : Exception
{
    public DataException(string message, Exception innerException)
        : base(message, innerException) { }
}

// ─── Performance Metrics ──────────────────────────────────────────────────────

/// <summary>Aggregated timing metrics for all measured operations.</summary>
public sealed class PerformanceMetrics
{
    public List<OperationMetric> Operations { get; set; } = new();
}

/// <summary>Timing statistics for a single named operation.</summary>
public sealed class OperationMetric
{
    public string OperationName    { get; set; } = string.Empty;
    public double AverageDurationMs { get; set; }
    public long   MaxDurationMs    { get; set; }
    public long   MinDurationMs    { get; set; }
    public int    ExecutionCount   { get; set; }
}
