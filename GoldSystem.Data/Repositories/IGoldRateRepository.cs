using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IGoldRateRepository : IRepository<GoldRate>
{
    Task<GoldRate?> GetLatestRateAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoldRate>> GetRateHistoryAsync(int branchId, int days, CancellationToken cancellationToken = default);
    Task<GoldRate?> GetRateByDateAsync(DateOnly date, int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoldRate>> GetRatesSinceAsync(DateTime since, int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoldRate>> GetManualOverridesAsync(int branchId, int days, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoldRate>> GetRatesBySourceAsync(string source, CancellationToken cancellationToken = default);
}
