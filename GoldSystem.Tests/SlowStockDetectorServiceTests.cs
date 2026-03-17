using GoldSystem.AI.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for SlowStockDetectorService.
/// </summary>
public class SlowStockDetectorServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;

    public SlowStockDetectorServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        _uow = new UnitOfWork(_context);
    }

    public void Dispose() => _context.Dispose();

    private static Item MakeItem(
        int id, int branchId, int categoryId, string status,
        int daysInStock, int? soldBillId = null) => new()
    {
        ItemId = id,
        TagNo = $"T{id:000}",
        Name = $"Item {id}",
        CategoryId = categoryId,
        BranchId = branchId,
        Status = status,
        Purity = "22K",
        MakingType = "PERCENT",
        PurchaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-daysInStock)),
        SoldBillId = soldBillId,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task DetectSlowStockAsync_ItemExceeds1Point5xAverage_IsIncluded()
    {
        // Category 1 average (from sold items): ~20 days.
        // Add 3 sold items around 20 days → avg ≈ 20 days.
        _context.Items.AddRange(
            MakeItem(1, branchId: 1, categoryId: 1, "Sold", daysInStock: 18, soldBillId: 10),
            MakeItem(2, branchId: 1, categoryId: 1, "Sold", daysInStock: 20, soldBillId: 11),
            MakeItem(3, branchId: 1, categoryId: 1, "Sold", daysInStock: 22, soldBillId: 12));

        // In-stock item that has been sitting for 40 days (> 1.5 × 20 = 30 days).
        _context.Items.Add(MakeItem(4, branchId: 1, categoryId: 1, "InStock", daysInStock: 40));
        await _context.SaveChangesAsync();

        var svc = new SlowStockDetectorService(_uow, NullLogger<SlowStockDetectorService>.Instance);

        // Act
        var alerts = await svc.DetectSlowStockAsync(branchId: 1);

        // Assert: exactly one slow item detected.
        Assert.Single(alerts);
        Assert.Equal(4, alerts[0].Item.ItemId);
        Assert.True(alerts[0].DaysInStock > alerts[0].CategoryAverageDays * 1.5);
    }

    [Fact]
    public async Task DetectSlowStockAsync_ItemBelowThreshold_NotIncluded()
    {
        // Average ≈ 20 days from sold items.
        _context.Items.AddRange(
            MakeItem(1, branchId: 1, categoryId: 1, "Sold", daysInStock: 20, soldBillId: 10),
            MakeItem(2, branchId: 1, categoryId: 1, "Sold", daysInStock: 20, soldBillId: 11));

        // In-stock item with only 25 days (< 1.5 × 20 = 30).
        _context.Items.Add(MakeItem(3, branchId: 1, categoryId: 1, "InStock", daysInStock: 25));
        await _context.SaveChangesAsync();

        var svc = new SlowStockDetectorService(_uow, NullLogger<SlowStockDetectorService>.Instance);

        // Act
        var alerts = await svc.DetectSlowStockAsync(branchId: 1);

        // Assert: no slow-moving items.
        Assert.Empty(alerts);
    }
}
