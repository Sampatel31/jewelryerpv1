using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(GoldDbContext context) : base(context) { }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
        => await DbSet.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);

    public async Task<Category?> GetCategoryByNameAsync(string name, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

    public async Task<IEnumerable<Category>> GetCategoriesWithItemsAsync(CancellationToken cancellationToken = default)
        => await DbSet.Include(c => c.Items).Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
}
