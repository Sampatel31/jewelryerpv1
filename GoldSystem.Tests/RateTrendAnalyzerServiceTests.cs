using GoldSystem.AI.Services;
using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for RateTrendAnalyzerService.
/// </summary>
public class RateTrendAnalyzerServiceTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly IUnitOfWork _uow;

    public RateTrendAnalyzerServiceTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        _uow = new UnitOfWork(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedRatesAsync(
        int branchId, int count, decimal startRate, decimal dailyIncrement)
    {
        var rates = Enumerable.Range(0, count).Select(i => new GoldRate
        {
            RateId = i + 1,
            RateDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-count + i)),
            RateTime = new TimeOnly(10, 0),
            Rate24K = startRate + dailyIncrement * i,
            Rate22K = startRate + dailyIncrement * i,
            Rate18K = (startRate + dailyIncrement * i) * 0.75m,
            Source = "MCX_SCRAPER",
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow
        });
        _context.GoldRates.AddRange(rates);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task AnalyzeTrendAsync_StronglyRisingRates_ReturnsUp()
    {
        // Seed 14 days of consistently rising rates (Δ +20/day → slope ≈ 20).
        await SeedRatesAsync(branchId: 1, count: 14, startRate: 5000m, dailyIncrement: 20m);
        var svc = new RateTrendAnalyzerService(_uow, NullLogger<RateTrendAnalyzerService>.Instance);

        var result = await svc.AnalyzeTrendAsync(branchId: 1);

        Assert.Equal("UP", result.Direction);
        Assert.True(result.Slope > 2m, $"Slope {result.Slope} should be > 2");
    }

    [Fact]
    public async Task AnalyzeTrendAsync_InsufficientData_ReturnsInsufficient()
    {
        // Seed only 5 days (below 14-day minimum).
        await SeedRatesAsync(branchId: 1, count: 5, startRate: 5000m, dailyIncrement: 5m);
        var svc = new RateTrendAnalyzerService(_uow, NullLogger<RateTrendAnalyzerService>.Instance);

        var result = await svc.AnalyzeTrendAsync(branchId: 1);

        Assert.Equal("INSUFFICIENT_DATA", result.Direction);
    }
}
