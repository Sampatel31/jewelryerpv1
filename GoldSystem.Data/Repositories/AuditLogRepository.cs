using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string? tableFilter,
        int? recordId,
        (DateTime From, DateTime To)? dateRange,
        int? branchId,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tableFilter))
            query = query.Where(a => a.TableName == tableFilter);

        if (recordId.HasValue)
            query = query.Where(a => a.RecordId == recordId.Value);

        if (dateRange.HasValue)
            query = query.Where(a => a.CreatedAt >= dateRange.Value.From && a.CreatedAt <= dateRange.Value.To);

        if (branchId.HasValue)
            query = query.Where(a => a.BranchId == branchId.Value);

        return await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByUserAsync(int userId, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(a => a.UserId == userId && a.CreatedAt >= cutoff)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByActionAsync(string action, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(a => a.Action == action && a.CreatedAt >= cutoff)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditsByTableAsync(string tableName, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Where(a => a.TableName == tableName && a.CreatedAt >= cutoff)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetChangeHistoryAsync(string tableName, int recordId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(a => a.TableName == tableName && a.RecordId == recordId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
}
