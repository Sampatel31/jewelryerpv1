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

    /// <summary>Sync status summary for all active non-owner branches.</summary>
    public async Task<IEnumerable<SyncStatusSummary>> GetSyncStatusAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var branches = await _context.Branches
            .Where(b => b.IsActive && b.BranchId == branchId)
            .ToListAsync(cancellationToken);

        var result = new List<SyncStatusSummary>();
        foreach (var branch in branches)
        {
            var pending = await _context.SyncQueues
                .CountAsync(s => s.BranchId == branch.BranchId && s.Status == "Pending", cancellationToken);
            var failed = await _context.SyncQueues
                .CountAsync(s => s.BranchId == branch.BranchId && s.Status == "Failed", cancellationToken);
            var lastSynced = await _context.SyncQueues
                .Where(s => s.BranchId == branch.BranchId && s.Status == "Synced")
                .MaxAsync(s => (DateTime?)s.SyncedAt, cancellationToken);

            result.Add(new SyncStatusSummary(branch.BranchId, branch.Name, pending, failed, lastSynced));
        }
        return result;
    }

    /// <summary>The most recent successful sync timestamp for a branch.</summary>
    public async Task<DateTime?> GetLastSuccessfulSyncAsync(int branchId, CancellationToken cancellationToken = default)
        => await _context.SyncQueues
            .Where(s => s.BranchId == branchId && s.Status == "Synced")
            .MaxAsync(s => (DateTime?)s.SyncedAt, cancellationToken);
}
