using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string? tableFilter, int? recordId, (DateTime From, DateTime To)? dateRange, int? branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditsByUserAsync(int userId, int days, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditsByActionAsync(string action, int days, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetAuditsByTableAsync(string tableName, int days, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetChangeHistoryAsync(string tableName, int recordId, CancellationToken cancellationToken = default);
}
