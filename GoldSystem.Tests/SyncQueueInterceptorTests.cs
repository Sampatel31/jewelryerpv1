using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Sync.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="SyncQueueInterceptor"/>.
/// The interceptor is wired into an in-memory DbContext so that SyncQueue rows
/// are created automatically when entities are saved.
/// </summary>
public class SyncQueueInterceptorTests : IDisposable
{
    private readonly GoldDbContext _context;

    public SyncQueueInterceptorTests()
    {
        var interceptor = new SyncQueueInterceptor(NullLogger<SyncQueueInterceptor>.Instance);

        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase($"SyncInterceptorTest_{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;

        _context = new GoldDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose() => _context.Dispose();

    // ── INSERT captured ────────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_NewVendor_CreatesPendingSyncQueueEntry()
    {
        var vendor = new Vendor
        {
            VendorId = 1001,
            Name = "Test Vendor",
            Phone = "9876543210",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        var queueEntries = _context.SyncQueues
            .Where(q => q.TableName == "Vendor" && q.RecordId == 1001)
            .ToList();

        Assert.NotEmpty(queueEntries);
        Assert.All(queueEntries, e => Assert.Equal("Pending", e.Status));
    }

    // ── UPDATE captured ────────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_UpdatedCustomer_CreatesUpdateSyncQueueEntry()
    {
        // First, add customer without interceptor interference by using raw Add+SaveChanges.
        var customer = new Customer
        {
            CustomerId = 2001,
            Name = "Original",
            Phone = "1111111111",
            BranchId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Clear the queue entries created by the insert.
        _context.SyncQueues.RemoveRange(_context.SyncQueues.ToList());
        await _context.SaveChangesAsync();

        // Now perform the update.
        customer.Name = "Updated";
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        var updateEntry = _context.SyncQueues
            .FirstOrDefault(q => q.TableName == "Customer" && q.RecordId == 2001 && q.Operation == "Update");

        Assert.NotNull(updateEntry);
        Assert.Equal("Pending", updateEntry!.Status);
    }

    // ── DELETE not captured ────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_DeletedVendor_DoesNotCreateSyncQueueEntry()
    {
        var vendor = new Vendor
        {
            VendorId = 3001,
            Name = "Temp Vendor",
            Phone = "0000000000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        // Clear existing queue entries.
        _context.SyncQueues.RemoveRange(_context.SyncQueues.ToList());
        await _context.SaveChangesAsync();

        // Delete the vendor.
        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();

        // No sync entry should be created for a hard delete.
        var deleteEntry = _context.SyncQueues
            .FirstOrDefault(q => q.TableName == "Vendor" && q.RecordId == 3001);

        Assert.Null(deleteEntry);
    }

    // ── JSON payload correctness ───────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_NewVendor_PayloadContainsEntityProperties()
    {
        var vendor = new Vendor
        {
            VendorId = 4001,
            Name = "Payload Vendor",
            Phone = "9999999999",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        var entry = _context.SyncQueues
            .FirstOrDefault(q => q.TableName == "Vendor" && q.RecordId == 4001);

        Assert.NotNull(entry);
        var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(entry!.Payload);
        Assert.NotNull(payload);
        Assert.True(payload!.ContainsKey("Name"));
    }

    // ── Status is Pending ──────────────────────────────────────────────────────

    [Fact]
    public async Task SavingChanges_NewBill_SyncQueueStatusIsPending()
    {
        var bill = new Bill
        {
            BillId = 5001,
            BillNo = "HO-001",
            BillDate = DateOnly.FromDateTime(DateTime.Today),
            CustomerId = 1,
            BranchId = 1,
            UserId = 1,
            Status = "Completed",
            PaymentMode = "Cash",
            CreatedAt = DateTime.UtcNow
        };

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var entry = _context.SyncQueues
            .FirstOrDefault(q => q.TableName == "Bill" && q.RecordId == 5001);

        Assert.NotNull(entry);
        Assert.Equal("Pending", entry!.Status);
    }

    // ── Non-syncable entity is NOT captured ────────────────────────────────────

    [Fact]
    public async Task SavingChanges_NewCategory_DoesNotCreateSyncQueueEntry()
    {
        var category = new Category
        {
            CategoryId = 6001,
            Name = "TestCat",
            DefaultMakingType = "PERCENT",
            DefaultMakingValue = 10m,
            DefaultWastagePercent = 2m,
            DefaultPurity = "22K",
            IsActive = true,
            SortOrder = 99
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var entry = _context.SyncQueues
            .FirstOrDefault(q => q.TableName == "Category");

        Assert.Null(entry);
    }

    // ── IsSyncableEntity helper ────────────────────────────────────────────────

    [Fact]
    public void IsSyncableEntity_SyncableTypes_ReturnsTrue()
    {
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(Item)));
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(Bill)));
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(BillItem)));
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(Customer)));
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(OldGoldExchange)));
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(Payment)));
        Assert.True(SyncQueueInterceptor.IsSyncableEntity(typeof(Vendor)));
    }

    [Fact]
    public void IsSyncableEntity_NonSyncableTypes_ReturnsFalse()
    {
        Assert.False(SyncQueueInterceptor.IsSyncableEntity(typeof(Category)));
        Assert.False(SyncQueueInterceptor.IsSyncableEntity(typeof(Branch)));
        Assert.False(SyncQueueInterceptor.IsSyncableEntity(typeof(GoldRate)));
        Assert.False(SyncQueueInterceptor.IsSyncableEntity(typeof(User)));
    }
}
