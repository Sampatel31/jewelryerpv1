using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IBillRepository : IRepository<Bill>
{
    Task<Bill?> GetByBillNoAsync(string billNo, CancellationToken cancellationToken = default);
    Task<Bill?> GetBillWithItemsAsync(int billId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetBillsByCustomerAsync(int customerId, int pageSize, int pageNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetBillsByDateRangeAsync(DateOnly fromDate, DateOnly toDate, int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetBillsByStatusAsync(string status, int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetBillsAfterAsync(DateTime createdAfter, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetUnlockedBillsAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetLockedBillsAsync(int branchId, CancellationToken cancellationToken = default);
    Task LockBillAsync(int billId, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyRevenueAsync(DateOnly date, int branchId, CancellationToken cancellationToken = default);
    Task<decimal> GetMonthlyRevenueAsync(int month, int year, int branchId, CancellationToken cancellationToken = default);
}
