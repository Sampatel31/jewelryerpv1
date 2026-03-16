using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerWithHistoryAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetTopCustomersByVolumeAsync(int topN, int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bill>> GetCustomerLedgerAsync(int customerId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetCreditCustomersAsync(int branchId, CancellationToken cancellationToken = default);
    Task UpdateTotalPurchasedAsync(int customerId, CancellationToken cancellationToken = default);
    Task UpdateLoyaltyPointsAsync(int customerId, int points, CancellationToken cancellationToken = default);
}
