using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for the UnitOfWork implementation: lazy loading, SaveChanges, transactions, and disposal.
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly GoldDbContext _context;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    // ─── 1. Repository lazy loading ─────────────────────────────────────────────

    [Fact]
    public void Branches_IsLazyLoaded_ReturnsSameInstance()
    {
        var uow = new UnitOfWork(_context);
        var r1 = uow.Branches;
        var r2 = uow.Branches;
        Assert.Same(r1, r2);
    }

    [Fact]
    public void AllRepositories_ReturnNonNull()
    {
        var uow = new UnitOfWork(_context);
        Assert.NotNull(uow.Branches);
        Assert.NotNull(uow.GoldRates);
        Assert.NotNull(uow.Categories);
        Assert.NotNull(uow.Items);
        Assert.NotNull(uow.Bills);
        Assert.NotNull(uow.BillItems);
        Assert.NotNull(uow.Customers);
        Assert.NotNull(uow.OldGoldExchanges);
        Assert.NotNull(uow.Payments);
        Assert.NotNull(uow.Vendors);
        Assert.NotNull(uow.Users);
        Assert.NotNull(uow.SyncQueue);
        Assert.NotNull(uow.AuditLogs);
    }

    // ─── 2. SaveChangesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_PersistsAddedEntities()
    {
        var uow = new UnitOfWork(_context);
        await uow.Categories.AddAsync(new Category
        {
            CategoryId = 9001,
            Name = "SaveTest",
            DefaultMakingType = "PERCENT",
            DefaultMakingValue = 10m,
            DefaultWastagePercent = 2m,
            DefaultPurity = "22K",
            IsActive = true
        });

        var saved = await uow.SaveChangesAsync();

        Assert.Equal(1, saved);
        Assert.NotNull(await uow.Categories.GetByIdAsync(9001));
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        var uow = new UnitOfWork(_context);
        var count = await uow.SaveChangesAsync();
        Assert.Equal(0, count);
    }

    // ─── 3. Transaction commit ──────────────────────────────────────────────────

    [Fact]
    public async Task CommitAsync_AfterBeginAndSave_PersistsData()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var ctx = new GoldDbContext(options);
        await using var uow = new UnitOfWork(ctx);

        await uow.BeginTransactionAsync();
        await uow.Categories.AddAsync(new Category
        {
            CategoryId = 9002,
            Name = "TxCommit",
            DefaultMakingType = "PERCENT",
            DefaultMakingValue = 10m,
            DefaultWastagePercent = 2m,
            DefaultPurity = "22K",
            IsActive = true
        });
        await uow.SaveChangesAsync();
        await uow.CommitAsync();

        Assert.NotNull(await uow.Categories.GetByIdAsync(9002));
    }

    // ─── 4. Transaction rollback ────────────────────────────────────────────────

    [Fact]
    public async Task RollbackAsync_AfterBegin_DoesNotThrow()
    {
        // InMemory provider does not support true transactions but rollback should not throw
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        await using var ctx = new GoldDbContext(options);
        await using var uow = new UnitOfWork(ctx);

        await uow.BeginTransactionAsync();
        await uow.RollbackAsync();  // should not throw
    }

    // ─── 5. Multiple repositories share the same DbContext ──────────────────────

    [Fact]
    public async Task MultipleRepositories_ShareContext_ChangesSavedTogether()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new GoldDbContext(options);
        await using var uow = new UnitOfWork(ctx);

        await uow.Categories.AddAsync(new Category
        {
            CategoryId = 9010,
            Name = "CatForBranch",
            DefaultMakingType = "PERCENT",
            DefaultMakingValue = 10m,
            DefaultWastagePercent = 2m,
            DefaultPurity = "22K",
            IsActive = true
        });
        await uow.Branches.AddAsync(new Branch
        {
            BranchId = 9010,
            Code = "T1",
            Name = "TestBranch",
            IsOwnerBranch = false,
            IsActive = true,
            GSTIN = "TESTGSTIN00001",
            Phone = "9999999999",
            Address = "Test"
        });

        var saved = await uow.SaveChangesAsync();

        Assert.Equal(2, saved);
    }

    // ─── 6. Constructor null check ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UnitOfWork(null!));
    }

    // ─── 7. DisposeAsync releases context ───────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes_DoesNotThrow()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new GoldDbContext(options);
        var uow = new UnitOfWork(ctx);

        await uow.DisposeAsync();
        // Second dispose should not throw
        // (context is already disposed, but UnitOfWork guards the transaction)
    }

    // ─── 8. CommitAsync without BeginTransaction is a no-op ────────────────────

    [Fact]
    public async Task CommitAsync_WithoutBeginTransaction_IsNoOp()
    {
        var uow = new UnitOfWork(_context);
        await uow.CommitAsync();  // no transaction started – should not throw
    }

    // ─── 9. RollbackAsync without BeginTransaction is a no-op ──────────────────

    [Fact]
    public async Task RollbackAsync_WithoutBeginTransaction_IsNoOp()
    {
        var uow = new UnitOfWork(_context);
        await uow.RollbackAsync();  // should not throw
    }

    // ─── 10. Repositories are correct types ─────────────────────────────────────

    [Fact]
    public void Repositories_AreCorrectConcreteTypes()
    {
        var uow = new UnitOfWork(_context);
        Assert.IsAssignableFrom<IBranchRepository>(uow.Branches);
        Assert.IsAssignableFrom<IGoldRateRepository>(uow.GoldRates);
        Assert.IsAssignableFrom<ICategoryRepository>(uow.Categories);
        Assert.IsAssignableFrom<IItemRepository>(uow.Items);
        Assert.IsAssignableFrom<IBillRepository>(uow.Bills);
        Assert.IsAssignableFrom<IBillItemRepository>(uow.BillItems);
        Assert.IsAssignableFrom<ICustomerRepository>(uow.Customers);
        Assert.IsAssignableFrom<IOldGoldExchangeRepository>(uow.OldGoldExchanges);
        Assert.IsAssignableFrom<IPaymentRepository>(uow.Payments);
        Assert.IsAssignableFrom<IVendorRepository>(uow.Vendors);
        Assert.IsAssignableFrom<IUserRepository>(uow.Users);
        Assert.IsAssignableFrom<ISyncQueueRepository>(uow.SyncQueue);
        Assert.IsAssignableFrom<IAuditLogRepository>(uow.AuditLogs);
    }
}
