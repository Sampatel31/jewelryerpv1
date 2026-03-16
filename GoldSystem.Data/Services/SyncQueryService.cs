using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Services;

/// <summary>
/// Specialized query service for sync-status reporting across branches.
/// </summary>
public class SyncQueryService
{
    private readonly GoldDbContext _context;

    public SyncQueryService(GoldDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public record SyncStatusSummary(
        int BranchId,
        string BranchName,
        int PendingCount,
        int FailedCount,
        DateTime? LastSyncedAt);

    /// <summary>Number of pending sync records for a branch.</summary>
    public async Task<int> GetPendingSyncCountAsync(int branchId, CancellationToken cancellationToken = default)
        => await _context.SyncQueues
            .CountAsync(s => s.BranchId == branchId && s.Status == "Pending", cancellationToken);

    /// <summary>Sync status summary for a specific branch.</summary>
    public async Task<IEnumerable<SyncStatusSummary>> GetSyncStatusAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches
            .Where(b => b.IsActive && b.BranchId == branchId)
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
            return Enumerable.Empty<SyncStatusSummary>();

        // Fetch all three counters for this branch in parallel
        var pendingTask = _context.SyncQueues.CountAsync(s => s.BranchId == branchId && s.Status == "Pending", cancellationToken);
        var failedTask  = _context.SyncQueues.CountAsync(s => s.BranchId == branchId && s.Status == "Failed",  cancellationToken);
        var lastSyncTask = _context.SyncQueues
            .Where(s => s.BranchId == branchId && s.Status == "Synced")
            .MaxAsync(s => (DateTime?)s.SyncedAt, cancellationToken);

        await Task.WhenAll(pendingTask, failedTask, lastSyncTask);

        return new[]
        {
            new SyncStatusSummary(branch.BranchId, branch.Name, pendingTask.Result, failedTask.Result, lastSyncTask.Result)
        };
    }

    /// <summary>The most recent successful sync timestamp for a branch.</summary>
    public async Task<DateTime?> GetLastSuccessfulSyncAsync(int branchId, CancellationToken cancellationToken = default)
        => await _context.SyncQueues
            .Where(s => s.BranchId == branchId && s.Status == "Synced")
            .MaxAsync(s => (DateTime?)s.SyncedAt, cancellationToken);
}
