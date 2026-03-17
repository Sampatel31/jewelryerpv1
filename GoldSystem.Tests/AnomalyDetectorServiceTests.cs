using GoldSystem.AI.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for AnomalyDetectorService.
/// </summary>
public class AnomalyDetectorServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;

    public AnomalyDetectorServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        _uow = new UnitOfWork(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedBillsAsync(int count, int branchId = 1, decimal baseAmount = 5000m)
    {
        var bills = Enumerable.Range(0, count).Select(i => new Bill
        {
            BillId = i + 1,
            BillNo = $"B{i + 1:000}",
            BillDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-i)),
            GrandTotal = baseAmount + (i % 5) * 200m,
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
    public async Task HasSufficientDataAsync_With50PlusBills_ReturnsTrue()
    {
        // Arrange: seed exactly 50 bills.
        await SeedBillsAsync(50);
        var svc = new AnomalyDetectorService(_uow, NullLogger<AnomalyDetectorService>.Instance);

        // Act
        var result = await svc.HasSufficientDataAsync(branchId: 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasSufficientDataAsync_WithFewerThan50Bills_ReturnsFalse()
    {
        // Arrange: only 10 bills.
        await SeedBillsAsync(10);
        var svc = new AnomalyDetectorService(_uow, NullLogger<AnomalyDetectorService>.Instance);

        // Act
        var result = await svc.HasSufficientDataAsync(branchId: 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_InsufficientData_ReturnsEmpty()
    {
        // Arrange: no bills in DB.
        var svc = new AnomalyDetectorService(_uow, NullLogger<AnomalyDetectorService>.Instance);

        // Act: detection without data should not throw.
        var anomalies = await svc.DetectAnomaliesAsync(branchId: 1);

        // Assert: graceful degradation.
        Assert.Empty(anomalies);
    }
}
