using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.RateEngine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoldSystem.RateEngine.Services;

/// <summary>
/// Persists and retrieves gold rates from the <see cref="GoldDbContext"/>.
/// </summary>
public class RateRepository
{
    private readonly IDbContextFactory<GoldDbContext> _dbFactory;
    private readonly ILogger<RateRepository> _logger;

    public RateRepository(IDbContextFactory<GoldDbContext> dbFactory, ILogger<RateRepository> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>Saves a new gold rate and returns the generated RateId.</summary>
    public async Task<int> SaveRateAsync(GoldRateResult rate, int branchId, int? createdByUserId = null,
        bool isManualOverride = false, string? overrideNote = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entity = new GoldRate
        {
            RateDate = DateOnly.FromDateTime(rate.FetchedAt),
            RateTime = TimeOnly.FromDateTime(rate.FetchedAt),
            Rate24K = rate.Rate24K,
            Rate22K = rate.Rate22K,
            Rate18K = rate.Rate18K,
            Source = rate.Source,
            IsManualOverride = isManualOverride,
            OverrideNote = overrideNote,
            BranchId = branchId,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        db.GoldRates.Add(entity);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Saved rate {RateId} for branch {BranchId}: 24K={Rate24K}", entity.RateId, branchId, rate.Rate24K);
        return entity.RateId;
    }

    /// <summary>Gets the most recent rate for the given branch.</summary>
    public async Task<GoldRate?> GetLatestRateAsync(int branchId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.GoldRates
            .Where(r => r.BranchId == branchId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Gets rate history for the given branch over the past <paramref name="days"/> days.</summary>
    public async Task<IReadOnlyList<GoldRate>> GetRateHistoryAsync(int branchId, int days, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        return await db.GoldRates
            .Where(r => r.BranchId == branchId && r.RateDate >= since)
            .OrderByDescending(r => r.RateDate).ThenByDescending(r => r.RateTime)
            .ToListAsync(ct);
    }

    /// <summary>Gets the rate for a specific date for the given branch.</summary>
    public async Task<GoldRate?> GetRateByDateAsync(DateOnly date, int branchId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.GoldRates
            .Where(r => r.BranchId == branchId && r.RateDate == date)
            .OrderByDescending(r => r.RateTime)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Soft-deletes (marks obsolete) a rate by setting its Source to "OBSOLETE".</summary>
    public async Task MarkAsObsoleteAsync(int rateId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var rate = await db.GoldRates.FindAsync(new object[] { rateId }, ct);
        if (rate is null) return;

        rate.Source = "OBSOLETE";
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Marked rate {RateId} as obsolete", rateId);
    }
}
