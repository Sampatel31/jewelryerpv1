using GoldSystem.AI.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for RestockSuggestionsService.
/// </summary>
public class RestockSuggestionsServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;

    public RestockSuggestionsServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        _uow = new UnitOfWork(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedCategoryAsync(int id, string name)
    {
        _context.Categories.Add(new Category
        {
            CategoryId = id,
            Name = name,
            DefaultMakingType = "PERCENT",
            DefaultMakingValue = 12m,
            DefaultWastagePercent = 2m,
            DefaultPurity = "22K",
            IsActive = true,
            SortOrder = id
        });
        await _context.SaveChangesAsync();
    }

    private async Task SeedBillWithItemsAsync(
        int billId, int branchId, int categoryId, int soldCount, int stockCount)
    {
        // Add the bill within the 90-day window.
        _context.Bills.Add(new Bill
        {
            BillId = billId,
            BillNo = $"B{billId:000}",
            BillDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
            GrandTotal = soldCount * 5000m,
            BranchId = branchId,
            CustomerId = 1,
            Status = "Completed",
            PaymentMode = "Cash",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        });

        // Add sold items linked to this bill.
        for (int i = 0; i < soldCount; i++)
        {
            _context.Items.Add(new Item
            {
                ItemId = billId * 1000 + i,
                TagNo = $"S{billId}-{i}",
                Name = $"Sold Item {i}",
                CategoryId = categoryId,
                BranchId = branchId,
                Status = "Sold",
                Purity = "22K",
                MakingType = "PERCENT",
                PurchaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
                SoldBillId = billId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Add in-stock items for the same category.
        for (int i = 0; i < stockCount; i++)
        {
            _context.Items.Add(new Item
            {
                ItemId = billId * 1000 + soldCount + i,
                TagNo = $"IS{billId}-{i}",
                Name = $"Stock Item {i}",
                CategoryId = categoryId,
                BranchId = branchId,
                Status = "InStock",
                Purity = "22K",
                MakingType = "PERCENT",
                PurchaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetRecommendationsAsync_HighVelocityCategory_IsRecommended()
    {
        // Arrange: Category 1 sold 10 items, only 2 in stock → velocity = 5 (≥ 2.0 threshold).
        await SeedCategoryAsync(1, "Rings");
        await SeedBillWithItemsAsync(billId: 1, branchId: 1, categoryId: 1, soldCount: 10, stockCount: 2);
        var svc = new RestockSuggestionsService(_uow, NullLogger<RestockSuggestionsService>.Instance);

        // Act
        var recs = await svc.GetRecommendationsAsync(branchId: 1);

        // Assert: category is recommended for restock.
        Assert.Single(recs);
        Assert.Equal(1, recs[0].CategoryId);
        Assert.True(recs[0].VelocityRatio >= 2.0m);
        Assert.True(recs[0].SuggestedOrderQty > 0);
    }

    [Fact]
    public async Task GetRecommendationsAsync_SuggestedQtyIsHalfOfSold()
    {
        // Arrange: sold 20, stock 1 → velocity = 20, suggested = ceil(20 * 0.5) = 10.
        await SeedCategoryAsync(2, "Bangles");
        await SeedBillWithItemsAsync(billId: 2, branchId: 1, categoryId: 2, soldCount: 20, stockCount: 1);
        var svc = new RestockSuggestionsService(_uow, NullLogger<RestockSuggestionsService>.Instance);

        // Act
        var recs = await svc.GetRecommendationsAsync(branchId: 1);

        // Assert: suggested quantity = ceil(20 * 0.5) = 10.
        var rec = Assert.Single(recs);
        Assert.Equal(10, rec.SuggestedOrderQty);
    }
}
