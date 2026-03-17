using GoldSystem.AI.Models;
using GoldSystem.AI.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for SalesForecastService.
/// </summary>
public class SalesForecastServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;

    public SalesForecastServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        _uow = new UnitOfWork(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedBillsAsync(int count, int branchId = 1)
    {
        var bills = Enumerable.Range(0, count).Select(i => new Bill
        {
            BillId = i + 1,
            BillNo = $"B{i + 1:000}",
            BillDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-i)),
            GrandTotal = 10000m + i * 500m,
            BranchId = branchId,
            CustomerId = 1,
            Status = "Completed",
            PaymentMode = "Cash",
            UserId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        });
        _context.Bills.AddRange(bills);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task HasSufficientDataAsync_WithEnoughBills_ReturnsTrue()
    {
        // Arrange: create 31 distinct bill dates (> 30 day minimum).
        await SeedBillsAsync(31);
        var svc = new SalesForecastService(_uow, NullLogger<SalesForecastService>.Instance);

        // Act
        var result = await svc.HasSufficientDataAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasSufficientDataAsync_WithInsufficientData_ReturnsFalse()
    {
        // Arrange: only 5 distinct dates — far below the 30-day minimum.
        await SeedBillsAsync(5);
        var svc = new SalesForecastService(_uow, NullLogger<SalesForecastService>.Instance);

        // Act
        var result = await svc.HasSufficientDataAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PredictNext7DaysAsync_InsufficientData_ReturnsEmpty()
    {
        // Arrange: no data in DB.
        var svc = new SalesForecastService(_uow, NullLogger<SalesForecastService>.Instance);

        // Act
        var results = await svc.PredictNext7DaysAsync();

        // Assert: graceful degradation — empty list, no exception.
        Assert.Empty(results);
    }
}
