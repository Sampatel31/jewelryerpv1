using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for InventoryQueryService, BillingQueryService, and SyncQueryService.
/// </summary>
public class QueryServiceTests : IDisposable
{
    private readonly GoldDbContext _context;

    public QueryServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        SeedBaseData();
    }

    public void Dispose() => _context.Dispose();

    private void SeedBaseData()
    {
        _context.Branches.Add(new Branch { BranchId = 100, Code = "QS", Name = "Query Branch", IsOwnerBranch = true, IsActive = true, GSTIN = "G100", Phone = "0", Address = "A" });
        _context.Categories.Add(new Category { CategoryId = 10, Name = "Ring", DefaultMakingType = "PERCENT", DefaultMakingValue = 12, DefaultWastagePercent = 2, DefaultPurity = "22K", IsActive = true, SortOrder = 1 });
        _context.Vendors.Add(new Vendor { VendorId = 10, Name = "V10", Phone = "0", IsActive = true });
        _context.Customers.Add(new Customer { CustomerId = 100, Name = "TestCustomer", Phone = "9000000001", BranchId = 100 });
        _context.Users.Add(new User { UserId = 100, Name = "QSUser", Username = "qsuser", PasswordHash = "h", Role = "Operator", BranchId = 100, IsActive = true });
        _context.SaveChanges();
    }

    // ─── InventoryQueryService ───────────────────────────────────────────────────

    [Fact]
    public async Task GetStockValuationAsync_ReturnsItemsWithMarketValue()
    {
        // 10g pure gold @ rate 75000/10g => market value = 75000
        _context.Items.Add(new Item { ItemId = 2001, TagNo = "QS001", CategoryId = 10, Name = "Ring1", Purity = "22K", GrossWeight = 10m, StoneWeight = 0, NetWeight = 10m, PureGoldWeight = 10m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 74000m, Status = "InStock", BranchId = 100, VendorId = 10, PurchaseDate = DateOnly.FromDateTime(DateTime.Today) });
        await _context.SaveChangesAsync();

        var svc = new InventoryQueryService(_context);
        var valuation = (await svc.GetStockValuationAsync(100, 75000m)).ToList();

        Assert.NotEmpty(valuation);
        var item = valuation.First(v => v.TagNo == "QS001");
        Assert.Equal(75000m, item.CurrentMarketValue);
        Assert.Equal(1000m, item.UnrealizedGain);  // 75000 - 74000
    }

    [Fact]
    public async Task GetStockAgeingAsync_CorrectlyBucketsItems()
    {
        var oldPurchase = DateOnly.FromDateTime(DateTime.Today.AddDays(-100));
        var recentPurchase = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));
        _context.Items.Add(new Item { ItemId = 2002, TagNo = "QS002", CategoryId = 10, Name = "Ring2", Purity = "22K", GrossWeight = 5m, StoneWeight = 0, NetWeight = 5m, PureGoldWeight = 5m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 37000m, Status = "InStock", BranchId = 100, VendorId = 10, PurchaseDate = oldPurchase });
        _context.Items.Add(new Item { ItemId = 2003, TagNo = "QS003", CategoryId = 10, Name = "Ring3", Purity = "22K", GrossWeight = 5m, StoneWeight = 0, NetWeight = 5m, PureGoldWeight = 5m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 37000m, Status = "InStock", BranchId = 100, VendorId = 10, PurchaseDate = recentPurchase });
        await _context.SaveChangesAsync();

        var svc = new InventoryQueryService(_context);
        var ageing = (await svc.GetStockAgeingAsync(100)).ToList();

        var oldItem = ageing.First(a => a.TagNo == "QS002");
        var newItem = ageing.First(a => a.TagNo == "QS003");

        Assert.Equal("90+ days", oldItem.AgeingBucket);
        Assert.Equal("0-30 days", newItem.AgeingBucket);
    }

    [Fact]
    public async Task GetSlowMovingItemsAsync_ReturnsOnlyOlderThanThreshold()
    {
        var oldPurchase = DateOnly.FromDateTime(DateTime.Today.AddDays(-60));
        var newPurchase = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));
        _context.Items.Add(new Item { ItemId = 2004, TagNo = "QS004", CategoryId = 10, Name = "Ring4", Purity = "22K", GrossWeight = 5m, StoneWeight = 0, NetWeight = 5m, PureGoldWeight = 5m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 37000m, Status = "InStock", BranchId = 100, VendorId = 10, PurchaseDate = oldPurchase });
        _context.Items.Add(new Item { ItemId = 2005, TagNo = "QS005", CategoryId = 10, Name = "Ring5", Purity = "22K", GrossWeight = 5m, StoneWeight = 0, NetWeight = 5m, PureGoldWeight = 5m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 37000m, Status = "InStock", BranchId = 100, VendorId = 10, PurchaseDate = newPurchase });
        await _context.SaveChangesAsync();

        var svc = new InventoryQueryService(_context);
        var slow = (await svc.GetSlowMovingItemsAsync(100, 30)).ToList();

        Assert.Contains(slow, a => a.TagNo == "QS004");
        Assert.DoesNotContain(slow, a => a.TagNo == "QS005");
    }

    [Fact]
    public async Task GetStockHealthAsync_ReturnsCorrectTotals()
    {
        _context.Items.Add(new Item { ItemId = 2006, TagNo = "QS006", CategoryId = 10, Name = "Ring6", Purity = "22K", GrossWeight = 10m, StoneWeight = 0, NetWeight = 10m, PureGoldWeight = 9.167m, MakingType = "PERCENT", MakingValue = 12m, WastagePercent = 2m, PurchaseRate24K = 75000m, CostPrice = 68000m, Status = "InStock", BranchId = 100, VendorId = 10, PurchaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)) });
        await _context.SaveChangesAsync();

        var svc = new InventoryQueryService(_context);
        var health = await svc.GetStockHealthAsync(100, 75000m);

        Assert.True(health.TotalItems >= 1);
        Assert.True(health.TotalNetWeight > 0);
        Assert.True(health.TotalMarketValue > 0);
    }

    // ─── BillingQueryService ────────────────────────────────────────────────────

    [Fact]
    public async Task GetDayBookAsync_ReturnsBillsForDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.Bills.Add(new Bill { BillId = 3001, BillNo = "DB-001", BillDate = today, CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "Cash", GrandTotal = 25000m, AmountPaid = 25000m, BalanceDue = 0m });
        _context.Bills.Add(new Bill { BillId = 3002, BillNo = "DB-002", BillDate = today.AddDays(-1), CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "UPI", GrandTotal = 15000m, AmountPaid = 15000m, BalanceDue = 0m });
        await _context.SaveChangesAsync();

        var svc = new BillingQueryService(_context);
        var dayBook = (await svc.GetDayBookAsync(today, 100)).ToList();

        Assert.Single(dayBook);
        Assert.Equal("DB-001", dayBook[0].BillNo);
    }

    [Fact]
    public async Task GetGSTSummaryAsync_IncludesCGSTAndSGST()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.Bills.Add(new Bill { BillId = 3003, BillNo = "GST-001", BillDate = today, CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "Cash", TaxableAmount = 70000m, CGST = 1050m, SGST = 1050m, IGST = 0m, GrandTotal = 72100m, AmountPaid = 72100m, BalanceDue = 0m });
        await _context.SaveChangesAsync();

        var svc = new BillingQueryService(_context);
        var gst = (await svc.GetGSTSummaryAsync(today, today, 100)).ToList();

        Assert.NotEmpty(gst);
        var entry = gst.First(g => g.BillNo == "GST-001");
        Assert.Equal(2100m, entry.TotalGST);
    }

    [Fact]
    public async Task GetPaymentModeSummaryAsync_GroupsByMode()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.Bills.Add(new Bill { BillId = 3004, BillNo = "PM-001", BillDate = today, CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "Cash", GrandTotal = 10000m });
        _context.Bills.Add(new Bill { BillId = 3005, BillNo = "PM-002", BillDate = today, CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "UPI", GrandTotal = 20000m });
        _context.Payments.Add(new Payment { PaymentId = 3001, BillId = 3004, Mode = "Cash", Amount = 10000m, PaymentDate = today });
        _context.Payments.Add(new Payment { PaymentId = 3002, BillId = 3005, Mode = "UPI", Amount = 20000m, PaymentDate = today });
        await _context.SaveChangesAsync();

        var svc = new BillingQueryService(_context);
        var summary = (await svc.GetPaymentModeSummaryAsync(today, today, 100)).ToList();

        Assert.Contains(summary, s => s.Mode == "Cash" && s.TotalAmount == 10000m);
        Assert.Contains(summary, s => s.Mode == "UPI" && s.TotalAmount == 20000m);
    }

    [Fact]
    public async Task GetCustomerLedgerAsync_ReturnsCustomerBillsInRange()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        _context.Bills.Add(new Bill { BillId = 3006, BillNo = "CL-001", BillDate = today, CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "Cash", GrandTotal = 5000m });
        _context.Bills.Add(new Bill { BillId = 3007, BillNo = "CL-002", BillDate = today.AddDays(-100), CustomerId = 100, BranchId = 100, UserId = 100, Status = "Completed", PaymentMode = "Cash", GrandTotal = 6000m });
        await _context.SaveChangesAsync();

        var svc = new BillingQueryService(_context);
        var ledger = (await svc.GetCustomerLedgerAsync(100, today.AddDays(-10), today)).ToList();

        Assert.Single(ledger);
        Assert.Equal("CL-001", ledger[0].BillNo);
    }

    // ─── SyncQueryService ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingSyncCountAsync_ReturnsCorrectCount()
    {
        _context.SyncQueues.Add(new SyncQueue { QueueId = 4001, BranchId = 100, TableName = "Items", RecordId = 1, Operation = "INSERT", Payload = "{}", Status = "Pending", CreatedAt = DateTime.UtcNow });
        _context.SyncQueues.Add(new SyncQueue { QueueId = 4002, BranchId = 100, TableName = "Bills", RecordId = 2, Operation = "INSERT", Payload = "{}", Status = "Pending", CreatedAt = DateTime.UtcNow });
        _context.SyncQueues.Add(new SyncQueue { QueueId = 4003, BranchId = 100, TableName = "Bills", RecordId = 3, Operation = "UPDATE", Payload = "{}", Status = "Synced", CreatedAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var svc = new SyncQueryService(_context);
        var count = await svc.GetPendingSyncCountAsync(100);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetLastSuccessfulSyncAsync_ReturnsLatestSyncedAt()
    {
        var syncTime = DateTime.UtcNow.AddHours(-1);
        _context.SyncQueues.Add(new SyncQueue { QueueId = 5001, BranchId = 100, TableName = "Items", RecordId = 1, Operation = "INSERT", Payload = "{}", Status = "Synced", CreatedAt = DateTime.UtcNow.AddHours(-2), SyncedAt = syncTime });
        await _context.SaveChangesAsync();

        var svc = new SyncQueryService(_context);
        var last = await svc.GetLastSuccessfulSyncAsync(100);

        Assert.NotNull(last);
        Assert.Equal(syncTime, last!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetLastSuccessfulSyncAsync_NoSyncedEntries_ReturnsNull()
    {
        var svc = new SyncQueryService(_context);
        var last = await svc.GetLastSuccessfulSyncAsync(999);
        Assert.Null(last);
    }

    [Fact]
    public async Task GetSyncStatusAsync_ReturnsSummaryForBranch()
    {
        _context.SyncQueues.Add(new SyncQueue { QueueId = 6001, BranchId = 100, TableName = "Items", RecordId = 1, Operation = "INSERT", Payload = "{}", Status = "Pending", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var svc = new SyncQueryService(_context);
        var status = (await svc.GetSyncStatusAsync(100)).ToList();

        Assert.NotEmpty(status);
        var entry = status.First(s => s.BranchId == 100);
        Assert.Equal(1, entry.PendingCount);
    }
}
