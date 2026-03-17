using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GoldSystem.WPF.Services;

/// <summary>
/// Wraps operations with a <see cref="Stopwatch"/> and accumulates per-operation
/// timing statistics that can be retrieved via <see cref="GetMetrics"/>.
/// Uses <see cref="ConcurrentBag{T}"/> for lock-free concurrent appends.
/// </summary>
public sealed class PerformanceService : IPerformanceService
{
    private readonly ILoggingService _loggingService;
    private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _operationTimes = new();

    public PerformanceService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await action();
        }
        finally
        {
            sw.Stop();
            Record(operation, sw.ElapsedMilliseconds);
        }
    }

    public async Task MeasureAsync(string operation, Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await action();
        }
        finally
        {
            sw.Stop();
            Record(operation, sw.ElapsedMilliseconds);
        }
    }

    public PerformanceMetrics GetMetrics()
    {
        var metrics = new PerformanceMetrics();

        foreach (var (name, bag) in _operationTimes)
        {
            // Take a snapshot to avoid interference from concurrent writes.
            var times = bag.ToArray();
            if (times.Length == 0) continue;

            metrics.Operations.Add(new OperationMetric
            {
                OperationName     = name,
                AverageDurationMs = times.Average(),
                MaxDurationMs     = times.Max(),
                MinDurationMs     = times.Min(),
                ExecutionCount    = times.Length
            });
        }

        return metrics;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void Record(string operation, long elapsedMs)
    {
        _loggingService.LogPerformance(operation, elapsedMs);

        var bag = _operationTimes.GetOrAdd(operation, _ => new ConcurrentBag<long>());
        bag.Add(elapsedMs);
    }
}
