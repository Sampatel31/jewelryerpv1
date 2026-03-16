using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class SyncQueueRepository : Repository<SyncQueue>, ISyncQueueRepository
{
    public SyncQueueRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<SyncQueue>> GetPendingSyncsAsync(int branchId, int limit = 100, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(s => s.BranchId == branchId && s.Status == "Pending")
            .OrderBy(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SyncQueue>> GetFailedSyncsAsync(int branchId, int limit = 100, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(s => s.BranchId == branchId && s.Status == "Failed")
            .OrderBy(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<SyncQueue>> GetSyncHistoryAsync(int branchId, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(s => s.BranchId == branchId && s.CreatedAt >= cutoff)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsSyncedAsync(long queueId, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.FindAsync(new object[] { queueId }, cancellationToken);
        if (entry is null) return;
        entry.Status = "Synced";
        entry.SyncedAt = DateTime.UtcNow;
    }

    public async Task MarkAsFailedAsync(long queueId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.FindAsync(new object[] { queueId }, cancellationToken);
        if (entry is null) return;
        // Status tracks the failure; error details are logged at the application layer
        entry.Status = "Failed";
    }

    public async Task<int> PurgeOldSyncsAsync(int branchId, int daysToKeep, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        var toDelete = await DbSet
            .Where(s => s.BranchId == branchId && s.Status == "Synced" && s.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);
        DbSet.RemoveRange(toDelete);
        return toDelete.Count;
    }
}
