using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IBillItemRepository : IRepository<BillItem>
{
    Task<IEnumerable<BillItem>> GetItemsByBillAsync(int billId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BillItem>> GetItemsByCustomerAsync(int customerId, int pageSize, CancellationToken cancellationToken = default);
}
