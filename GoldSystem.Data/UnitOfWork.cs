using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoldSystem.Data;

/// <summary>
/// Unit of Work implementation – coordinates all repositories and manages transactions.
/// All repositories are lazy-loaded to avoid unnecessary allocations.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly GoldDbContext _context;
    private IDbContextTransaction? _transaction;

    private IBranchRepository? _branches;
    private IGoldRateRepository? _goldRates;
    private ICategoryRepository? _categories;
    private IItemRepository? _items;
    private IBillRepository? _bills;
    private IBillItemRepository? _billItems;
    private ICustomerRepository? _customers;
    private IOldGoldExchangeRepository? _oldGoldExchanges;
    private IPaymentRepository? _payments;
    private IVendorRepository? _vendors;
    private IUserRepository? _users;
    private ISyncQueueRepository? _syncQueue;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(GoldDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IBranchRepository Branches
        => _branches ??= new BranchRepository(_context);

    public IGoldRateRepository GoldRates
        => _goldRates ??= new GoldRateRepository(_context);

    public ICategoryRepository Categories
        => _categories ??= new CategoryRepository(_context);

    public IItemRepository Items
        => _items ??= new ItemRepository(_context);

    public IBillRepository Bills
        => _bills ??= new BillRepository(_context);

    public IBillItemRepository BillItems
        => _billItems ??= new BillItemRepository(_context);

    public ICustomerRepository Customers
        => _customers ??= new CustomerRepository(_context);

    public IOldGoldExchangeRepository OldGoldExchanges
        => _oldGoldExchanges ??= new OldGoldExchangeRepository(_context);

    public IPaymentRepository Payments
        => _payments ??= new PaymentRepository(_context);

    public IVendorRepository Vendors
        => _vendors ??= new VendorRepository(_context);

    public IUserRepository Users
        => _users ??= new UserRepository(_context);

    public ISyncQueueRepository SyncQueue
        => _syncQueue ??= new SyncQueueRepository(_context);

    public IAuditLogRepository AuditLogs
        => _auditLogs ??= new AuditLogRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        await _context.DisposeAsync();
    }
}
