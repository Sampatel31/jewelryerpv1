using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface ISyncQueueRepository : IRepository<SyncQueue>
{
    Task<IEnumerable<SyncQueue>> GetPendingSyncsAsync(int branchId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<SyncQueue>> GetFailedSyncsAsync(int branchId, int limit = 100, CancellationToken cancellationToken = default);
    Task<IEnumerable<SyncQueue>> GetSyncHistoryAsync(int branchId, int days, CancellationToken cancellationToken = default);
    Task MarkAsSyncedAsync(long queueId, CancellationToken cancellationToken = default);
    Task MarkAsFailedAsync(long queueId, string errorMessage, CancellationToken cancellationToken = default);
    Task<int> PurgeOldSyncsAsync(int branchId, int daysToKeep, CancellationToken cancellationToken = default);
}
