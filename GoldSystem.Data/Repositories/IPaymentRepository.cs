using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetPaymentsByBillAsync(int billId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentHistoryAsync(int branchId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByModeAsync(string mode, int branchId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}
