using GoldSystem.Data;
using GoldSystem.RateEngine.Models;
using GoldSystem.RateEngine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for <see cref="RateRepository"/> using an in-memory EF Core database.
/// </summary>
public class RateRepositoryTests : IDisposable
{
    private readonly GoldDbContext _db;
    private readonly IDbContextFactory<GoldDbContext> _factory;
    private readonly RateRepository _repo;

    public RateRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase($"RateRepoTest_{Guid.NewGuid()}")
            .Options;

        _db = new GoldDbContext(options);
        _db.Database.EnsureCreated();

        _factory = new TestDbContextFactory(options);
        _repo = new RateRepository(_factory, NullLogger<RateRepository>.Instance);
    }

    public void Dispose() => _db.Dispose();

    // ── SaveRateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveRateAsync_ValidRate_ReturnsPositiveRateId()
    {
        var rate = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", DateTime.UtcNow);

        var id = await _repo.SaveRateAsync(rate, branchId: 1);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task SaveRateAsync_ValidRate_PersistsAllFields()
    {
        var fetchedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var rate = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", fetchedAt);

        var id = await _repo.SaveRateAsync(rate, branchId: 1);

        await using var db = await _factory.CreateDbContextAsync();
        var saved = await db.GoldRates.FindAsync(id);

        Assert.NotNull(saved);
        Assert.Equal(72000m, saved.Rate24K);
        Assert.Equal(66000m, saved.Rate22K);
        Assert.Equal(54000m, saved.Rate18K);
        Assert.Equal("MCX_SCRAPER", saved.Source);
        Assert.Equal(1, saved.BranchId);
        Assert.False(saved.IsManualOverride);
    }

    [Fact]
    public async Task SaveRateAsync_ManualOverride_SetsIsManualOverrideTrue()
    {
        var rate = new GoldRateResult(70000m, 64166.67m, 52500m, "MANUAL_OVERRIDE", DateTime.UtcNow);

        var id = await _repo.SaveRateAsync(rate, branchId: 1, isManualOverride: true, overrideNote: "Test override");

        await using var db = await _factory.CreateDbContextAsync();
        var saved = await db.GoldRates.FindAsync(id);
        Assert.NotNull(saved);
        Assert.True(saved.IsManualOverride);
        Assert.Equal("Test override", saved.OverrideNote);
    }

    // ── GetLatestRateAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetLatestRateAsync_NoRates_ReturnsNull()
    {
        var latest = await _repo.GetLatestRateAsync(branchId: 999);
        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatestRateAsync_MultipleRates_ReturnsMostRecent()
    {
        var older = new GoldRateResult(70000m, 64166.67m, 52500m, "MCX_SCRAPER",
            DateTime.UtcNow.AddMinutes(-10));
        var newer = new GoldRateResult(71000m, 65083.33m, 53250m, "MCX_SCRAPER",
            DateTime.UtcNow);

        await _repo.SaveRateAsync(older, branchId: 1);
        await _repo.SaveRateAsync(newer, branchId: 1);

        var latest = await _repo.GetLatestRateAsync(branchId: 1);

        Assert.NotNull(latest);
        Assert.Equal(71000m, latest.Rate24K);
    }

    // ── GetRateHistoryAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateHistoryAsync_ReturnsOnlyRatesWithinDayRange()
    {
        // Save a rate for today and one from 40 days ago
        var recent = new GoldRateResult(72000m, 66000m, 54000m, "MCX_SCRAPER", DateTime.UtcNow);
        await _repo.SaveRateAsync(recent, branchId: 1);

        // Directly insert an old rate bypassing the repository to control RateDate
        await using var db = await _factory.CreateDbContextAsync();
        db.GoldRates.Add(new Data.Entities.GoldRate
        {
            RateDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)),
            RateTime = TimeOnly.MinValue,
            Rate24K = 60000m, Rate22K = 55000m, Rate18K = 45000m,
            Source = "MCX_SCRAPER",
            BranchId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        });
        await db.SaveChangesAsync();

        var history = await _repo.GetRateHistoryAsync(branchId: 1, days: 30);

        Assert.All(history, r => Assert.NotEqual(60000m, r.Rate24K));
    }

    // ── GetRateByDateAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateByDateAsync_ExistingDate_ReturnsRate()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var db = await _factory.CreateDbContextAsync();
        db.GoldRates.Add(new Data.Entities.GoldRate
        {
            RateDate = today,
            RateTime = new TimeOnly(9, 0),
            Rate24K = 73000m, Rate22K = 67083.33m, Rate18K = 54750m,
            Source = "MCX_SCRAPER",
            BranchId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await _repo.GetRateByDateAsync(today, branchId: 1);

        Assert.NotNull(result);
        Assert.Equal(73000m, result.Rate24K);
    }

    [Fact]
    public async Task GetRateByDateAsync_NonExistingDate_ReturnsNull()
    {
        var result = await _repo.GetRateByDateAsync(new DateOnly(2000, 1, 1), branchId: 1);
        Assert.Null(result);
    }

    // ── helper factory ────────────────────────────────────────────────────────

    private sealed class TestDbContextFactory : IDbContextFactory<GoldDbContext>
    {
        private readonly DbContextOptions<GoldDbContext> _options;
        public TestDbContextFactory(DbContextOptions<GoldDbContext> options) => _options = options;
        public GoldDbContext CreateDbContext() => new(_options);
    }
}
