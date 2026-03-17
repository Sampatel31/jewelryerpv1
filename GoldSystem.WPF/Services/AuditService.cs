using GoldSystem.Core.Interfaces;
using GoldSystem.Core.Models;
using System.Text;

namespace GoldSystem.WPF.Services;

/// <summary>
/// In-memory audit service.  Stores all log entries in a thread-safe list
/// and exports them to a simple CSV-formatted Excel-compatible file.
/// </summary>
public sealed class AuditService : IAuditService
{
    private readonly List<AuditLog> _logs = new();
    private readonly object         _lock = new();
    private          int            _nextId = 1;

    // -- Log Action -----------------------------------------------------------

    public Task LogActionAsync(
        int      userId,
        string   action,
        string   module,
        string   entity,
        string   entityId,
        string?  oldValue  = null,
        string?  newValue  = null,
        string   ipAddress = "",
        string   userAgent = "",
        CancellationToken ct = default)
    {
        var entry = new AuditLog(
            Id:        _nextId++,
            UserId:    userId,
            Action:    action,
            Module:    module,
            Entity:    entity,
            EntityId:  entityId,
            Timestamp: DateTime.UtcNow,
            OldValue:  oldValue,
            NewValue:  newValue,
            IpAddress: ipAddress,
            UserAgent: userAgent);

        lock (_lock) { _logs.Add(entry); }
        return Task.CompletedTask;
    }

    // -- Query ----------------------------------------------------------------

    public Task<IReadOnlyList<AuditLog>> GetAuditLogsAsync(
        int?      userId     = null,
        string?   action     = null,
        string?   module     = null,
        DateTime? from       = null,
        DateTime? to         = null,
        int       maxRecords = 500,
        CancellationToken ct = default)
    {
        IEnumerable<AuditLog> query;
        lock (_lock) { query = _logs.ToList(); }

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action.Contains(action, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(l => l.Module.Equals(module, StringComparison.OrdinalIgnoreCase));

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        var result = query
            .OrderByDescending(l => l.Timestamp)
            .Take(maxRecords)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditLog>>(result);
    }

    // -- Export ---------------------------------------------------------------

    public Task<byte[]> ExportAuditTrailAsync(
        IReadOnlyList<AuditLog> logs,
        CancellationToken ct = default)
    {
        // Produces a UTF-8 CSV that Excel opens natively
        var sb = new StringBuilder();
        sb.AppendLine("Id,UserId,Action,Module,Entity,EntityId,Timestamp,OldValue,NewValue,IpAddress");

        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(',',
                log.Id,
                log.UserId,
                Csv(log.Action),
                Csv(log.Module),
                Csv(log.Entity),
                Csv(log.EntityId),
                log.FormattedTimestamp,
                Csv(log.OldValue ?? string.Empty),
                Csv(log.NewValue ?? string.Empty),
                Csv(log.IpAddress)));
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    // -- Cleanup --------------------------------------------------------------

    public Task CleanupOldLogsAsync(int keepDays = 365, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-keepDays);
        lock (_lock) { _logs.RemoveAll(l => l.Timestamp < cutoff); }
        return Task.CompletedTask;
    }

    // -- Helper ---------------------------------------------------------------

    private static string Csv(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
