using GoldSystem.Data.Repositories;

namespace GoldSystem.Data;

/// <summary>
/// Unit of Work pattern - coordinates all repository operations within a single transaction scope.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IBranchRepository Branches { get; }
    IGoldRateRepository GoldRates { get; }
    ICategoryRepository Categories { get; }
    IItemRepository Items { get; }
    IBillRepository Bills { get; }
    IBillItemRepository BillItems { get; }
    ICustomerRepository Customers { get; }
    IOldGoldExchangeRepository OldGoldExchanges { get; }
    IPaymentRepository Payments { get; }
    IVendorRepository Vendors { get; }
    IUserRepository Users { get; }
    ISyncQueueRepository SyncQueue { get; }
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
