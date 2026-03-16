using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface IItemRepository : IRepository<Item>
{
    Task<Item?> GetByHUIDAsync(string huid, CancellationToken cancellationToken = default);
    Task<Item?> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task<IEnumerable<Item>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Item>> GetInventoryByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Item>> GetInventoryByBranchAndCategoryAsync(int branchId, int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Item>> GetInStockItemsAsync(int branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Item>> GetSlowMovingItemsAsync(int categoryId, int daysThreshold, CancellationToken cancellationToken = default);
    Task<decimal> GetItemValueAsync(int itemId, decimal currentRate24K, CancellationToken cancellationToken = default);
    Task<decimal> GetStockValueByBranchAsync(int branchId, decimal currentRate24K, CancellationToken cancellationToken = default);
    Task<IEnumerable<(string Purity, decimal TotalWeight, int Count)>> GetStockByPurityAsync(int branchId, CancellationToken cancellationToken = default);
    Task<Item?> GetItemWithCategoryAsync(int itemId, CancellationToken cancellationToken = default);
    Task MarkAsSoldAsync(int itemId, int billId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int itemId, string newStatus, CancellationToken cancellationToken = default);
}
