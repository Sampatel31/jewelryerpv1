using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class BillItemRepository : Repository<BillItem>, IBillItemRepository
{
    public BillItemRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<BillItem>> GetItemsByBillAsync(int billId, CancellationToken cancellationToken = default)
        => await DbSet.Where(bi => bi.BillId == billId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<BillItem>> GetItemsByCustomerAsync(int customerId, int pageSize, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(bi => bi.Bill)
            .Where(bi => bi.Bill.CustomerId == customerId)
            .OrderByDescending(bi => bi.Bill.BillDate)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
