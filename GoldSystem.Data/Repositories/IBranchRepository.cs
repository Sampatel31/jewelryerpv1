using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IBranchRepository : IRepository<Branch>
{
    Task<Branch?> GetOwnerBranchAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Branch>> GetSyncEligibleBranchesAsync(CancellationToken cancellationToken = default);
    Task<Branch?> GetBranchWithRatesAsync(int branchId, CancellationToken cancellationToken = default);
}
