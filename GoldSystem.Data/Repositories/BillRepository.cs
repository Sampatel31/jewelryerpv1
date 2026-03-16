using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class BillRepository : Repository<Bill>, IBillRepository
{
    public BillRepository(GoldDbContext context) : base(context) { }

    public async Task<Bill?> GetByBillNoAsync(string billNo, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(b => b.BillNo == billNo, cancellationToken);

    public async Task<Bill?> GetBillWithItemsAsync(int billId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(b => b.BillItems).ThenInclude(bi => bi.Item)
            .Include(b => b.Payments)
            .Include(b => b.OldGoldExchanges)
            .Include(b => b.Customer)
            .FirstOrDefaultAsync(b => b.BillId == billId, cancellationToken);

    public async Task<IEnumerable<Bill>> GetBillsByCustomerAsync(int customerId, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BillDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bill>> GetBillsByDateRangeAsync(DateOnly fromDate, DateOnly toDate, int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.BranchId == branchId && b.BillDate >= fromDate && b.BillDate <= toDate)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bill>> GetBillsByStatusAsync(string status, int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.BranchId == branchId && b.Status == status)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bill>> GetBillsAfterAsync(DateTime createdAfter, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.CreatedAt > createdAfter)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bill>> GetUnlockedBillsAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.BranchId == branchId && !b.IsLocked)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Bill>> GetLockedBillsAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.BranchId == branchId && b.IsLocked)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

    public async Task LockBillAsync(int billId, CancellationToken cancellationToken = default)
    {
        var bill = await DbSet.FindAsync(new object[] { billId }, cancellationToken);
        if (bill is null) return;
        bill.IsLocked = true;
    }

    public async Task<decimal> GetDailyRevenueAsync(DateOnly date, int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.BranchId == branchId && b.BillDate == date)
            .SumAsync(b => b.GrandTotal, cancellationToken);

    public async Task<decimal> GetMonthlyRevenueAsync(int month, int year, int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(b => b.BranchId == branchId && b.BillDate.Month == month && b.BillDate.Year == year)
            .SumAsync(b => b.GrandTotal, cancellationToken);
}
