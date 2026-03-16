using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for entity-specific repositories: domain-query methods.
/// </summary>
public class SpecializedRepositoryTests : IDisposable
{
    private readonly GoldDbContext _context;

    public SpecializedRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    // ─── BranchRepository ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOwnerBranchAsync_ReturnsOwnerBranch()
    {
        _context.Branches.Add(new Branch { BranchId = 5001, Code = "OWN", Name = "Owner", IsOwnerBranch = true, IsActive = true, GSTIN = "G1", Phone = "0", Address = "A" });
        _context.Branches.Add(new Branch { BranchId = 5002, Code = "B1", Name = "Branch1", IsOwnerBranch = false, IsActive = true, GSTIN = "G2", Phone = "0", Address = "A" });
        await _context.SaveChangesAsync();

        var repo = new BranchRepository(_context);
        var owner = await repo.GetOwnerBranchAsync();

        Assert.NotNull(owner);
        Assert.True(owner!.IsOwnerBranch);
    }

    [Fact]
    public async Task GetActiveBranchesAsync_ReturnsOnlyActiveBranches()
    {
        _context.Branches.Add(new Branch { BranchId = 5003, Code = "ACT", Name = "Active", IsOwnerBranch = false, IsActive = true, GSTIN = "G3", Phone = "0", Address = "A" });
        _context.Branches.Add(new Branch { BranchId = 5004, Code = "INA", Name = "Inactive", IsOwnerBranch = false, IsActive = false, GSTIN = "G4", Phone = "0", Address = "A" });
        await _context.SaveChangesAsync();

        var repo = new BranchRepository(_context);
        var active = (await repo.GetActiveBranchesAsync()).ToList();

        Assert.All(active, b => Assert.True(b.IsActive));
    }

    [Fact]
    public async Task GetByCodeAsync_ExistingCode_ReturnsBranch()
    {
        _context.Branches.Add(new Branch { BranchId = 5005, Code = "XYZ", Name = "XYZ Branch", IsOwnerBranch = false, IsActive = true, GSTIN = "G5", Phone = "0", Address = "A" });
        await _context.SaveChangesAsync();

        var repo = new BranchRepository(_context);
        var branch = await repo.GetByCodeAsync("XYZ");

        Assert.NotNull(branch);
        Assert.Equal("XYZ Branch", branch!.Name);
    }

    // ─── GoldRateRepository ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestRateAsync_ReturnsNewestRate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.GoldRates.Add(new GoldRate { RateId = 6001, BranchId = 1, RateDate = today.AddDays(-1), RateTime = new TimeOnly(9, 0), Rate24K = 74000m, Rate22K = 68000m, Rate18K = 55000m, Source = "MCX", CreatedAt = DateTime.UtcNow.AddDays(-1) });
        _context.GoldRates.Add(new GoldRate { RateId = 6002, BranchId = 1, RateDate = today, RateTime = new TimeOnly(9, 0), Rate24K = 75000m, Rate22K = 68750m, Rate18K = 56250m, Source = "MCX", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var repo = new GoldRateRepository(_context);
        var latest = await repo.GetLatestRateAsync(1);

        Assert.NotNull(latest);
        Assert.Equal(75000m, latest!.Rate24K);
    }

    [Fact]
    public async Task GetRateHistoryAsync_ReturnsRatesWithinDays()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.GoldRates.Add(new GoldRate { RateId = 6003, BranchId = 1, RateDate = today.AddDays(-2), RateTime = new TimeOnly(9, 0), Rate24K = 73000m, Rate22K = 67000m, Rate18K = 54000m, Source = "MCX", CreatedAt = DateTime.UtcNow });
        _context.GoldRates.Add(new GoldRate { RateId = 6004, BranchId = 1, RateDate = today.AddDays(-10), RateTime = new TimeOnly(9, 0), Rate24K = 72000m, Rate22K = 66000m, Rate18K = 54000m, Source = "MCX", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var repo = new GoldRateRepository(_context);
        var history = (await repo.GetRateHistoryAsync(1, 5)).ToList();

        Assert.All(history, r => Assert.Equal(1, r.BranchId));
        Assert.DoesNotContain(history, r => r.RateId == 6004);
    }

    // ─── ItemRepository ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByHUIDAsync_ExistingHUID_ReturnsItem()
    {
        _context.Categories.Add(new Category { CategoryId = 1, Name = "Ring", DefaultMakingType = "PERCENT", DefaultMakingValue = 12, DefaultWastagePercent = 2, DefaultPurity = "22K", IsActive = true });
        _context.Branches.Add(new Branch { BranchId = 1, Code = "HO", Name = "HO", IsOwnerBranch = true, IsActive = true, GSTIN = "G", Phone = "0", Address = "A" });
        _context.Vendors.Add(new Vendor { VendorId = 1, Name = "V", Phone = "0", IsActive = true });
        _context.Items.Add(new Item { ItemId = 7001, HUID = "ABC123", TagNo = "T001", CategoryId = 1, Name = "Ring1", Purity = "22K", GrossWeight = 10m, StoneWeight = 0m, NetWeight = 10m, PureGoldWeight = 9.167m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 68000m, Status = "InStock", BranchId = 1, VendorId = 1, PurchaseDate = DateOnly.FromDateTime(DateTime.Today) });
        await _context.SaveChangesAsync();

        var repo = new ItemRepository(_context);
        var item = await repo.GetByHUIDAsync("ABC123");

        Assert.NotNull(item);
        Assert.Equal(7001, item!.ItemId);
    }

    [Fact]
    public async Task GetInStockItemsAsync_ReturnsOnlyInStockItems()
    {
        _context.Categories.Add(new Category { CategoryId = 2, Name = "Chain", DefaultMakingType = "PERCENT", DefaultMakingValue = 10, DefaultWastagePercent = 2, DefaultPurity = "22K", IsActive = true });
        _context.Branches.Add(new Branch { BranchId = 2, Code = "B2", Name = "B2", IsOwnerBranch = false, IsActive = true, GSTIN = "G2", Phone = "0", Address = "A" });
        _context.Vendors.Add(new Vendor { VendorId = 2, Name = "V2", Phone = "0", IsActive = true });
        _context.Items.Add(new Item { ItemId = 7002, TagNo = "T002", CategoryId = 2, Name = "Chain1", Purity = "22K", GrossWeight = 8m, StoneWeight = 0m, NetWeight = 8m, PureGoldWeight = 7.33m, MakingType = "PERCENT", MakingValue = 10m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 55000m, Status = "InStock", BranchId = 2, VendorId = 2, PurchaseDate = DateOnly.FromDateTime(DateTime.Today) });
        _context.Items.Add(new Item { ItemId = 7003, TagNo = "T003", CategoryId = 2, Name = "Chain2", Purity = "22K", GrossWeight = 6m, StoneWeight = 0m, NetWeight = 6m, PureGoldWeight = 5.5m, MakingType = "PERCENT", MakingValue = 10m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 41000m, Status = "Sold", BranchId = 2, VendorId = 2, PurchaseDate = DateOnly.FromDateTime(DateTime.Today) });
        await _context.SaveChangesAsync();

        var repo = new ItemRepository(_context);
        var inStock = (await repo.GetInStockItemsAsync(2)).ToList();

        Assert.Single(inStock);
        Assert.Equal("InStock", inStock[0].Status);
    }

    [Fact]
    public async Task GetStockValueByBranchAsync_CalculatesCorrectValue()
    {
        _context.Categories.Add(new Category { CategoryId = 3, Name = "Bangle", DefaultMakingType = "FIXED", DefaultMakingValue = 2000, DefaultWastagePercent = 2, DefaultPurity = "24K", IsActive = true });
        _context.Branches.Add(new Branch { BranchId = 3, Code = "B3", Name = "B3", IsOwnerBranch = false, IsActive = true, GSTIN = "G3", Phone = "0", Address = "A" });
        _context.Vendors.Add(new Vendor { VendorId = 3, Name = "V3", Phone = "0", IsActive = true });
        // 10g pure gold in stock
        _context.Items.Add(new Item { ItemId = 7004, TagNo = "T004", CategoryId = 3, Name = "Bangle1", Purity = "24K", GrossWeight = 10m, StoneWeight = 0m, NetWeight = 10m, PureGoldWeight = 10m, MakingType = "FIXED", MakingValue = 2000m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 76000m, Status = "InStock", BranchId = 3, VendorId = 3, PurchaseDate = DateOnly.FromDateTime(DateTime.Today) });
        await _context.SaveChangesAsync();

        var repo = new ItemRepository(_context);
        // rate = 75000 / 10g; 10g * (75000/10) = 75000
        var value = await repo.GetStockValueByBranchAsync(3, 75000m);

        Assert.Equal(75000m, value);
    }

    [Fact]
    public async Task MarkAsSoldAsync_UpdatesStatusAndBillId()
    {
        _context.Categories.Add(new Category { CategoryId = 4, Name = "Earring", DefaultMakingType = "PERCENT", DefaultMakingValue = 15, DefaultWastagePercent = 2, DefaultPurity = "22K", IsActive = true });
        _context.Branches.Add(new Branch { BranchId = 4, Code = "B4", Name = "B4", IsOwnerBranch = false, IsActive = true, GSTIN = "G4", Phone = "0", Address = "A" });
        _context.Vendors.Add(new Vendor { VendorId = 4, Name = "V4", Phone = "0", IsActive = true });
        _context.Items.Add(new Item { ItemId = 7005, TagNo = "T005", CategoryId = 4, Name = "Earring1", Purity = "22K", GrossWeight = 5m, StoneWeight = 0m, NetWeight = 5m, PureGoldWeight = 4.58m, MakingType = "PERCENT", MakingValue = 15m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 36000m, Status = "InStock", BranchId = 4, VendorId = 4, PurchaseDate = DateOnly.FromDateTime(DateTime.Today) });
        await _context.SaveChangesAsync();

        var repo = new ItemRepository(_context);
        await repo.MarkAsSoldAsync(7005, 999);
        await _context.SaveChangesAsync();

        var item = await repo.GetByIdAsync(7005);
        Assert.Equal("Sold", item!.Status);
        Assert.Equal(999, item.SoldBillId);
    }

    // ─── BillRepository ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByBillNoAsync_ExistingBillNo_ReturnsBill()
    {
        _context.Branches.Add(new Branch { BranchId = 10, Code = "HB", Name = "HB", IsOwnerBranch = true, IsActive = true, GSTIN = "G10", Phone = "0", Address = "A" });
        _context.Customers.Add(new Customer { CustomerId = 10, Name = "Cust1", Phone = "1234567890", BranchId = 10 });
        _context.Users.Add(new User { UserId = 10, Name = "User1", Username = "u1", PasswordHash = "h", Role = "Operator", BranchId = 10, IsActive = true });
        _context.Bills.Add(new Bill { BillId = 8001, BillNo = "BILL-001", BillDate = DateOnly.FromDateTime(DateTime.Today), CustomerId = 10, BranchId = 10, UserId = 10, Status = "Completed", PaymentMode = "Cash", GrandTotal = 50000m });
        await _context.SaveChangesAsync();

        var repo = new BillRepository(_context);
        var bill = await repo.GetByBillNoAsync("BILL-001");

        Assert.NotNull(bill);
        Assert.Equal(8001, bill!.BillId);
    }

    [Fact]
    public async Task GetBillWithItemsAsync_IncludesRelated()
    {
        _context.Branches.Add(new Branch { BranchId = 11, Code = "HB2", Name = "HB2", IsOwnerBranch = true, IsActive = true, GSTIN = "G11", Phone = "0", Address = "A" });
        _context.Customers.Add(new Customer { CustomerId = 11, Name = "Cust2", Phone = "1234567891", BranchId = 11 });
        _context.Users.Add(new User { UserId = 11, Name = "User2", Username = "u2", PasswordHash = "h", Role = "Operator", BranchId = 11, IsActive = true });
        var bill = new Bill { BillId = 8002, BillNo = "BILL-002", BillDate = DateOnly.FromDateTime(DateTime.Today), CustomerId = 11, BranchId = 11, UserId = 11, Status = "Completed", PaymentMode = "Cash", GrandTotal = 30000m };
        _context.Bills.Add(bill);
        _context.Payments.Add(new Payment { PaymentId = 8001, BillId = 8002, Mode = "Cash", Amount = 30000m, PaymentDate = DateOnly.FromDateTime(DateTime.Today) });
        await _context.SaveChangesAsync();

        var repo = new BillRepository(_context);
        var loaded = await repo.GetBillWithItemsAsync(8002);

        Assert.NotNull(loaded);
        Assert.NotEmpty(loaded!.Payments);
    }

    [Fact]
    public async Task GetDailyRevenueAsync_SumsGrandTotalsForDate()
    {
        _context.Branches.Add(new Branch { BranchId = 12, Code = "HB3", Name = "HB3", IsOwnerBranch = true, IsActive = true, GSTIN = "G12", Phone = "0", Address = "A" });
        _context.Customers.Add(new Customer { CustomerId = 12, Name = "Cust3", Phone = "1234567892", BranchId = 12 });
        _context.Users.Add(new User { UserId = 12, Name = "User3", Username = "u3", PasswordHash = "h", Role = "Operator", BranchId = 12, IsActive = true });
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.Bills.Add(new Bill { BillId = 8003, BillNo = "BILL-003", BillDate = today, CustomerId = 12, BranchId = 12, UserId = 12, Status = "Completed", PaymentMode = "Cash", GrandTotal = 20000m });
        _context.Bills.Add(new Bill { BillId = 8004, BillNo = "BILL-004", BillDate = today, CustomerId = 12, BranchId = 12, UserId = 12, Status = "Completed", PaymentMode = "UPI", GrandTotal = 30000m });
        await _context.SaveChangesAsync();

        var repo = new BillRepository(_context);
        var revenue = await repo.GetDailyRevenueAsync(today, 12);

        Assert.Equal(50000m, revenue);
    }

    [Fact]
    public async Task LockBillAsync_SetIsLockedTrue()
    {
        _context.Branches.Add(new Branch { BranchId = 13, Code = "HB4", Name = "HB4", IsOwnerBranch = true, IsActive = true, GSTIN = "G13", Phone = "0", Address = "A" });
        _context.Customers.Add(new Customer { CustomerId = 13, Name = "Cust4", Phone = "1234567893", BranchId = 13 });
        _context.Users.Add(new User { UserId = 13, Name = "User4", Username = "u4", PasswordHash = "h", Role = "Operator", BranchId = 13, IsActive = true });
        _context.Bills.Add(new Bill { BillId = 8005, BillNo = "BILL-005", BillDate = DateOnly.FromDateTime(DateTime.Today), CustomerId = 13, BranchId = 13, UserId = 13, Status = "Completed", PaymentMode = "Cash", GrandTotal = 10000m, IsLocked = false });
        await _context.SaveChangesAsync();

        var repo = new BillRepository(_context);
        await repo.LockBillAsync(8005);
        await _context.SaveChangesAsync();

        var bill = await repo.GetByIdAsync(8005);
        Assert.True(bill!.IsLocked);
    }

    // ─── CustomerRepository ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByPhoneAsync_ExistingPhone_ReturnsCustomer()
    {
        _context.Branches.Add(new Branch { BranchId = 20, Code = "CB", Name = "CB", IsOwnerBranch = false, IsActive = true, GSTIN = "G20", Phone = "0", Address = "A" });
        _context.Customers.Add(new Customer { CustomerId = 9001, Name = "Priya", Phone = "9876543210", BranchId = 20 });
        await _context.SaveChangesAsync();

        var repo = new CustomerRepository(_context);
        var customer = await repo.GetByPhoneAsync("9876543210");

        Assert.NotNull(customer);
        Assert.Equal("Priya", customer!.Name);
    }

    [Fact]
    public async Task UpdateLoyaltyPointsAsync_AddsPoints()
    {
        _context.Branches.Add(new Branch { BranchId = 21, Code = "CB2", Name = "CB2", IsOwnerBranch = false, IsActive = true, GSTIN = "G21", Phone = "0", Address = "A" });
        _context.Customers.Add(new Customer { CustomerId = 9002, Name = "Amit", Phone = "9876543211", BranchId = 21, LoyaltyPoints = 100 });
        await _context.SaveChangesAsync();

        var repo = new CustomerRepository(_context);
        await repo.UpdateLoyaltyPointsAsync(9002, 50);
        await _context.SaveChangesAsync();

        var customer = await repo.GetByIdAsync(9002);
        Assert.Equal(150, customer!.LoyaltyPoints);
    }

    // ─── SyncQueueRepository ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingSyncsAsync_ReturnsPendingOnly()
    {
        _context.Branches.Add(new Branch { BranchId = 30, Code = "SB", Name = "SB", IsOwnerBranch = false, IsActive = true, GSTIN = "G30", Phone = "0", Address = "A" });
        _context.SyncQueues.Add(new SyncQueue { QueueId = 1001, BranchId = 30, TableName = "Items", RecordId = 1, Operation = "INSERT", Payload = "{}", Status = "Pending", CreatedAt = DateTime.UtcNow });
        _context.SyncQueues.Add(new SyncQueue { QueueId = 1002, BranchId = 30, TableName = "Items", RecordId = 2, Operation = "INSERT", Payload = "{}", Status = "Synced", CreatedAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var repo = new SyncQueueRepository(_context);
        var pending = (await repo.GetPendingSyncsAsync(30)).ToList();

        Assert.Single(pending);
        Assert.Equal("Pending", pending[0].Status);
    }

    [Fact]
    public async Task MarkAsSyncedAsync_SetsStatusAndSyncedAt()
    {
        _context.Branches.Add(new Branch { BranchId = 31, Code = "SB2", Name = "SB2", IsOwnerBranch = false, IsActive = true, GSTIN = "G31", Phone = "0", Address = "A" });
        _context.SyncQueues.Add(new SyncQueue { QueueId = 2001, BranchId = 31, TableName = "Bills", RecordId = 5, Operation = "INSERT", Payload = "{}", Status = "Pending", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var repo = new SyncQueueRepository(_context);
        await repo.MarkAsSyncedAsync(2001);
        await _context.SaveChangesAsync();

        var found = await _context.SyncQueues.FindAsync(2001L);
        Assert.Equal("Synced", found!.Status);
        Assert.NotNull(found.SyncedAt);
    }

    [Fact]
    public async Task MarkAsFailedAsync_SetsStatusToFailed()
    {
        _context.Branches.Add(new Branch { BranchId = 32, Code = "SB3", Name = "SB3", IsOwnerBranch = false, IsActive = true, GSTIN = "G32", Phone = "0", Address = "A" });
        _context.SyncQueues.Add(new SyncQueue { QueueId = 3001, BranchId = 32, TableName = "GoldRates", RecordId = 7, Operation = "INSERT", Payload = "{}", Status = "Pending", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var repo = new SyncQueueRepository(_context);
        await repo.MarkAsFailedAsync(3001, "Connection timeout");
        await _context.SaveChangesAsync();

        var found = await _context.SyncQueues.FindAsync(3001L);
        Assert.Equal("Failed", found!.Status);
    }
}
