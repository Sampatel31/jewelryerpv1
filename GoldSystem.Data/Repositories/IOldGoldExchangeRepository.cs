using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IOldGoldExchangeRepository : IRepository<OldGoldExchange>
{
    Task<IEnumerable<OldGoldExchange>> GetExchangesByBillAsync(int billId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OldGoldExchange>> GetExchangeHistoryAsync(int branchId, int days, CancellationToken cancellationToken = default);
}
