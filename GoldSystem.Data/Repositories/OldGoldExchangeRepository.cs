using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class OldGoldExchangeRepository : Repository<OldGoldExchange>, IOldGoldExchangeRepository
{
    public OldGoldExchangeRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<OldGoldExchange>> GetExchangesByBillAsync(int billId, CancellationToken cancellationToken = default)
        => await DbSet.Where(e => e.BillId == billId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<OldGoldExchange>> GetExchangeHistoryAsync(int branchId, int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await DbSet
            .Include(e => e.Bill)
            .Where(e => e.Bill.BranchId == branchId && e.CreatedAt >= cutoff)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
