using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Sync.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GoldSystem.Sync.Services;

/// <summary>
/// EF Core SaveChangesInterceptor that creates SyncQueue entries for each INSERT/UPDATE
/// on syncable entities.  Runs BEFORE changes are persisted so that the sync record and
/// the real change land in the same database transaction.
/// </summary>
public class SyncQueueInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<SyncQueueInterceptor> _logger;

    public SyncQueueInterceptor(ILogger<SyncQueueInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var branchId = GetCurrentBranchId(context);

        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Where(e => IsSyncableEntity(e.Entity.GetType()))
            .ToList();

        foreach (var entry in entries)
        {
            var queueEntry = CreateSyncQueueEntry(entry, branchId);
            context.Set<SyncQueue>().Add(queueEntry);
            _logger.LogInformation("Queued {Table} {Id} for sync",
                queueEntry.TableName, queueEntry.RecordId);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static SyncQueue CreateSyncQueueEntry(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        int branchId)
    {
        var entityType = entry.Entity.GetType();
        var tableName = entityType.Name;
        var operation = entry.State == EntityState.Added ? "Insert" : "Update";

        var data = new Dictionary<string, object>();
        foreach (var prop in entityType.GetProperties())
        {
            // Only capture simple (non-navigation) properties tracked by EF
            if (entry.Properties.Any(p => p.Metadata.Name == prop.Name))
            {
                var value = entry.CurrentValues[prop.Name];
                data[prop.Name] = value ?? string.Empty;
            }
        }

        var payload = JsonSerializer.Serialize(data);

        // For Added entities, the PK may not yet be database-assigned; use 0 as sentinel.
        var recordId = entry.CurrentValues.Properties
            .Where(p => p.IsPrimaryKey())
            .Select(p => entry.CurrentValues[p] as int?)
            .FirstOrDefault() ?? 0;

        return new SyncQueue
        {
            TableName = tableName,
            RecordId = recordId,
            Operation = operation,
            Payload = payload,
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };
    }

    public static bool IsSyncableEntity(Type type)
    {
        var syncableTypes = new HashSet<Type>
        {
            typeof(Item),
            typeof(Bill),
            typeof(BillItem),
            typeof(Customer),
            typeof(OldGoldExchange),
            typeof(Payment),
            typeof(Vendor)
        };
        return syncableTypes.Contains(type);
    }

    private static int GetCurrentBranchId(DbContext context)
    {
        // Prefer environment variable so branch PCs can be configured independently.
        var envValue = Environment.GetEnvironmentVariable("BRANCH_ID");
        if (int.TryParse(envValue, out var envBranchId))
            return envBranchId;

        // Fallback: first user in context (owner branch default = 1)
        return context.Set<User>().Local.FirstOrDefault()?.BranchId ?? 1;
    }
}
