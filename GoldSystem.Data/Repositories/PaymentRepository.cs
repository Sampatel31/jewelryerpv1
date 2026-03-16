using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<Payment>> GetPaymentsByBillAsync(int billId, CancellationToken cancellationToken = default)
        => await DbSet.Where(p => p.BillId == billId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Payment>> GetPaymentHistoryAsync(int branchId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.Bill)
            .Where(p => p.Bill.BranchId == branchId && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Payment>> GetPaymentsByModeAsync(string mode, int branchId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.Bill)
            .Where(p => p.Mode == mode && p.Bill.BranchId == branchId && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
}
