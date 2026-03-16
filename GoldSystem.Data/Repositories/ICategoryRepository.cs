using GoldSystem.Data.Entities;

namespace GoldSystem.Data.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetCategoriesWithItemsAsync(CancellationToken cancellationToken = default);
}
