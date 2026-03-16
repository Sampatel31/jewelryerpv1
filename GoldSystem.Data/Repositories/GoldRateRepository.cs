using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class GoldRateRepository : Repository<GoldRate>, IGoldRateRepository
{
    public GoldRateRepository(GoldDbContext context) : base(context) { }

    public async Task<GoldRate?> GetLatestRateAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(r => r.BranchId == branchId)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.RateTime)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IEnumerable<GoldRate>> GetRateHistoryAsync(int branchId, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-days));
        return await DbSet
            .Where(r => r.BranchId == branchId && r.RateDate >= cutoff)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.RateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<GoldRate?> GetRateByDateAsync(DateOnly date, int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(r => r.BranchId == branchId && r.RateDate == date)
            .OrderByDescending(r => r.RateTime)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IEnumerable<GoldRate>> GetRatesSinceAsync(DateTime since, int branchId, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateOnly.FromDateTime(since.Date);
        return await DbSet
            .Where(r => r.BranchId == branchId && r.RateDate >= cutoffDate)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.RateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GoldRate>> GetManualOverridesAsync(int branchId, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-days));
        return await DbSet
            .Where(r => r.BranchId == branchId && r.IsManualOverride && r.RateDate >= cutoff)
            .OrderByDescending(r => r.RateDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GoldRate>> GetRatesBySourceAsync(string source, CancellationToken cancellationToken = default)
        => await DbSet.Where(r => r.Source == source).OrderByDescending(r => r.RateDate).ToListAsync(cancellationToken);
}
