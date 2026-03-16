using GoldSystem.Core.Services;
using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Services;

/// <summary>
/// Appends immutable audit log entries for every significant data change.
/// </summary>
public class AuditLogger : IAuditLogger
{
    private readonly IUnitOfWork _uow;

    public AuditLogger(IUnitOfWork uow)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task LogAsync(int userId, string action, string tableName, int recordId,
        string? oldValueJson = null, string? newValueJson = null, int branchId = 0)
    {
        var auditEntry = new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.AuditLogs.AddAsync(auditEntry);
        await _uow.SaveChangesAsync();
    }
}
