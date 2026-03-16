using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(GoldDbContext context) : base(context) { }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Phone == phone, cancellationToken);

    public async Task<Customer?> GetCustomerWithHistoryAsync(int customerId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(c => c.Bills.OrderByDescending(b => b.BillDate).Take(50))
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

    public async Task<IEnumerable<Customer>> GetTopCustomersByVolumeAsync(int topN, int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(c => c.BranchId == branchId)
            .OrderByDescending(c => c.TotalPurchased)
            .Take(topN)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bill>> GetCustomerLedgerAsync(int customerId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
        => await Context.Bills
            .Where(b => b.CustomerId == customerId && b.BillDate >= fromDate && b.BillDate <= toDate)
            .OrderBy(b => b.BillDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Customer>> GetCreditCustomersAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(c => c.BranchId == branchId && c.CreditLimit > 0)
            .OrderByDescending(c => c.TotalPurchased)
            .ToListAsync(cancellationToken);

    public async Task UpdateTotalPurchasedAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var total = await Context.Bills
            .Where(b => b.CustomerId == customerId)
            .SumAsync(b => b.GrandTotal, cancellationToken);

        var customer = await DbSet.FindAsync(new object[] { customerId }, cancellationToken);
        if (customer is null) return;
        customer.TotalPurchased = total;
    }

    public async Task UpdateLoyaltyPointsAsync(int customerId, int points, CancellationToken cancellationToken = default)
    {
        var customer = await DbSet.FindAsync(new object[] { customerId }, cancellationToken);
        if (customer is null) return;
        customer.LoyaltyPoints += points;
    }
}
