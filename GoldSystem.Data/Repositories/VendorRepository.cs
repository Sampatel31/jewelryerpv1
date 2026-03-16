using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class VendorRepository : Repository<Vendor>, IVendorRepository
{
    public VendorRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<Vendor>> GetActiveVendorsAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(v => v.IsActive).OrderBy(v => v.Name).ToListAsync(cancellationToken);

    public async Task<Vendor?> GetVendorWithItemsAsync(int vendorId, CancellationToken cancellationToken = default)
        => await DbSet.Include(v => v.Items).FirstOrDefaultAsync(v => v.VendorId == vendorId, cancellationToken);

    public async Task<IEnumerable<Item>> GetVendorItemsAsync(int vendorId, CancellationToken cancellationToken = default)
        => await Context.Items
            .Include(i => i.Category)
            .Where(i => i.VendorId == vendorId)
            .OrderBy(i => i.TagNo)
            .ToListAsync(cancellationToken);

    public async Task UpdateOpeningBalanceAsync(int vendorId, decimal balance, CancellationToken cancellationToken = default)
    {
        var vendor = await DbSet.FindAsync(new object[] { vendorId }, cancellationToken);
        if (vendor is null) return;
        vendor.OpeningBalance = balance;
    }
}
