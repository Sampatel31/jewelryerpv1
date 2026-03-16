using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class BranchRepository : Repository<Branch>, IBranchRepository
{
    public BranchRepository(GoldDbContext context) : base(context) { }

    public async Task<Branch?> GetOwnerBranchAsync(CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(b => b.IsOwnerBranch, cancellationToken);

    public async Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(cancellationToken);

    public async Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(b => b.Code == code, cancellationToken);

    public async Task<IEnumerable<Branch>> GetSyncEligibleBranchesAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(b => b.IsActive && !b.IsOwnerBranch).ToListAsync(cancellationToken);

    public async Task<Branch?> GetBranchWithRatesAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(b => b.GoldRates.OrderByDescending(r => r.RateDate).Take(10))
            .FirstOrDefaultAsync(b => b.BranchId == branchId, cancellationToken);
}
