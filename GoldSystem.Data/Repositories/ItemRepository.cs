using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class ItemRepository : Repository<Item>, IItemRepository
{
    public ItemRepository(GoldDbContext context) : base(context) { }

    public async Task<Item?> GetByHUIDAsync(string huid, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(i => i.HUID == huid, cancellationToken);

    public async Task<Item?> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(i => i.TagNo == tag, cancellationToken);

    public async Task<IEnumerable<Item>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        => await DbSet.Where(i => i.Status == status).ToListAsync(cancellationToken);

    public async Task<IEnumerable<Item>> GetInventoryByBranchAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(i => i.Category)
            .Where(i => i.BranchId == branchId)
            .OrderBy(i => i.Category.SortOrder)
            .ThenBy(i => i.TagNo)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Item>> GetInventoryByBranchAndCategoryAsync(int branchId, int categoryId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(i => i.Category)
            .Where(i => i.BranchId == branchId && i.CategoryId == categoryId)
            .OrderBy(i => i.TagNo)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Item>> GetInStockItemsAsync(int branchId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(i => i.Category)
            .Where(i => i.BranchId == branchId && i.Status == "InStock")
            .OrderBy(i => i.TagNo)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Item>> GetSlowMovingItemsAsync(int categoryId, int daysThreshold, CancellationToken cancellationToken = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(-daysThreshold));
        return await DbSet
            .Include(i => i.Category)
            .Where(i => i.CategoryId == categoryId && i.Status == "InStock" && i.PurchaseDate <= cutoff)
            .OrderBy(i => i.PurchaseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetItemValueAsync(int itemId, decimal currentRate24K, CancellationToken cancellationToken = default)
    {
        var item = await DbSet.FindAsync(new object[] { itemId }, cancellationToken);
        if (item is null) return 0m;
        var ratePerGram = currentRate24K / 10m;
        return item.PureGoldWeight * ratePerGram;
    }

    public async Task<decimal> GetStockValueByBranchAsync(int branchId, decimal currentRate24K, CancellationToken cancellationToken = default)
    {
        var ratePerGram = currentRate24K / 10m;
        var totalPureGold = await DbSet
            .Where(i => i.BranchId == branchId && i.Status == "InStock")
            .SumAsync(i => i.PureGoldWeight, cancellationToken);
        return totalPureGold * ratePerGram;
    }

    public async Task<IEnumerable<(string Purity, decimal TotalWeight, int Count)>> GetStockByPurityAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .Where(i => i.BranchId == branchId && i.Status == "InStock")
            .GroupBy(i => i.Purity)
            .Select(g => new { Purity = g.Key, TotalWeight = g.Sum(i => i.NetWeight), Count = g.Count() })
            .ToListAsync(cancellationToken);
        return result.Select(r => (r.Purity, r.TotalWeight, r.Count));
    }

    public async Task<Item?> GetItemWithCategoryAsync(int itemId, CancellationToken cancellationToken = default)
        => await DbSet.Include(i => i.Category).FirstOrDefaultAsync(i => i.ItemId == itemId, cancellationToken);

    public async Task MarkAsSoldAsync(int itemId, int billId, CancellationToken cancellationToken = default)
    {
        var item = await DbSet.FindAsync(new object[] { itemId }, cancellationToken);
        if (item is null) return;
        item.Status = "Sold";
        item.SoldBillId = billId;
    }

    public async Task UpdateStatusAsync(int itemId, string newStatus, CancellationToken cancellationToken = default)
    {
        var item = await DbSet.FindAsync(new object[] { itemId }, cancellationToken);
        if (item is null) return;
        item.Status = newStatus;
    }
}
