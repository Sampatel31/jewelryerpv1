using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IVendorRepository : IRepository<Vendor>
{
    Task<IEnumerable<Vendor>> GetActiveVendorsAsync(CancellationToken cancellationToken = default);
    Task<Vendor?> GetVendorWithItemsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Item>> GetVendorItemsAsync(int vendorId, CancellationToken cancellationToken = default);
    Task UpdateOpeningBalanceAsync(int vendorId, decimal balance, CancellationToken cancellationToken = default);
}
