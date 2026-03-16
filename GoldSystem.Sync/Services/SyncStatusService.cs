using GoldSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Sync.Services;

/// <summary>
/// Provides real-time sync health and queue statistics for the dashboard.
/// </summary>
public interface ISyncStatusService
{
    Task<SyncStatusDto> GetStatusAsync(int branchId);
    Task<SyncQueueStatsDto> GetQueueStatsAsync(int branchId);
}

public class SyncStatusService : ISyncStatusService
{
    private readonly IUnitOfWork _uow;

    public SyncStatusService(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<SyncStatusDto> GetStatusAsync(int branchId)
    {
        var pendingCount = await _uow.SyncQueue.CountAsync(
            q => q.BranchId == branchId && q.Status == "Pending");

        var failedCount = await _uow.SyncQueue.CountAsync(
            q => q.BranchId == branchId && q.Status == "Failed");

        var lastSyncedRecord = await _uow.SyncQueue.FirstOrDefaultAsync(
            q => q.BranchId == branchId && q.Status == "Synced");

        return new SyncStatusDto(
            BranchId: branchId,
            PendingCount: pendingCount,
            FailedCount: failedCount,
            LastSyncedAt: lastSyncedRecord?.SyncedAt,
            IsHealthy: pendingCount < 100 && failedCount < 10);
    }

    public async Task<SyncQueueStatsDto> GetQueueStatsAsync(int branchId)
    {
        var stats = await _uow.SyncQueue
            .AsQueryable()
            .Where(q => q.BranchId == branchId)
            .GroupBy(q => q.TableName)
            .Select(g => new TableSyncStat(
                g.Key,
                g.Count(q => q.Status == "Pending"),
                g.Count(q => q.Status == "Synced"),
                g.Count(q => q.Status == "Failed")))
            .ToListAsync();

        return new SyncQueueStatsDto(stats.Cast<object>().ToList());
    }
}

public record SyncStatusDto(
    int BranchId,
    int PendingCount,
    int FailedCount,
    DateTime? LastSyncedAt,
    bool IsHealthy);

public record SyncQueueStatsDto(
    List<object> TableStatistics);

internal record TableSyncStat(
    string TableName,
    int Pending,
    int Synced,
    int Failed);
