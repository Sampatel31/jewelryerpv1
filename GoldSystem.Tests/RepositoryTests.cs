using GoldSystem.Data;
using GoldSystem.Data.Entities;
using GoldSystem.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Tests;

/// <summary>
/// Unit tests for the generic Repository&lt;TEntity&gt; implementation using an in-memory database.
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly GoldDbContext _context;
    private readonly Repository<Category> _repo;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new GoldDbContext(options);
        _repo = new Repository<Category>(_context);
    }

    public void Dispose() => _context.Dispose();

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static Category MakeCategory(int id, string name, bool active = true) => new()
    {
        CategoryId = id,
        Name = name,
        DefaultMakingType = "PERCENT",
        DefaultMakingValue = 12m,
        DefaultWastagePercent = 2m,
        DefaultPurity = "22K",
        IsActive = active,
        SortOrder = id
    };

    // ─── 1. GetByIdAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        var cat = MakeCategory(101, "Ring");
        await _repo.AddAsync(cat);
        await _context.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(101);

        Assert.NotNull(result);
        Assert.Equal("Ring", result!.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(9999);
        Assert.Null(result);
    }

    // ─── 2. GetAllAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        await _repo.AddRangeAsync(new[] { MakeCategory(201, "A"), MakeCategory(202, "B") });
        await _context.SaveChangesAsync();

        var all = (await _repo.GetAllAsync()).ToList();

        // InMemory DB includes seeded data; check our additions are present
        Assert.Contains(all, c => c.Name == "A");
        Assert.Contains(all, c => c.Name == "B");
    }

    // ─── 3. FindAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task FindAsync_WithMatchingPredicate_ReturnsMatchingEntities()
    {
        await _repo.AddRangeAsync(new[] { MakeCategory(301, "Active1"), MakeCategory(302, "Inactive", active: false) });
        await _context.SaveChangesAsync();

        var active = (await _repo.FindAsync(c => c.IsActive && c.CategoryId >= 301)).ToList();

        Assert.Single(active);
        Assert.Equal("Active1", active[0].Name);
    }

    [Fact]
    public async Task FindAsync_NullPredicate_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repo.FindAsync(null!));
    }

    // ─── 4. FirstOrDefaultAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task FirstOrDefaultAsync_Matching_ReturnsFirstMatch()
    {
        await _repo.AddAsync(MakeCategory(401, "Alpha"));
        await _context.SaveChangesAsync();

        var result = await _repo.FirstOrDefaultAsync(c => c.Name == "Alpha");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsNull()
    {
        var result = await _repo.FirstOrDefaultAsync(c => c.Name == "NonExistent");
        Assert.Null(result);
    }

    // ─── 5. CountAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CountAsync_NoPredicate_ReturnsTotalCount()
    {
        // Clear and populate a fresh context
        var opts = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new GoldDbContext(opts);
        var repo = new Repository<Category>(ctx);

        await repo.AddRangeAsync(new[] { MakeCategory(1, "X"), MakeCategory(2, "Y") });
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await repo.CountAsync());
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsFilteredCount()
    {
        await _repo.AddRangeAsync(new[] { MakeCategory(501, "P", active: true), MakeCategory(502, "Q", active: false) });
        await _context.SaveChangesAsync();

        var count = await _repo.CountAsync(c => !c.IsActive && c.CategoryId >= 500);

        Assert.Equal(1, count);
    }

    // ─── 6. AddAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_NullEntity_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repo.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ValidEntity_IsPersisted()
    {
        var cat = MakeCategory(601, "Pendant");
        await _repo.AddAsync(cat);
        await _context.SaveChangesAsync();

        Assert.NotNull(await _repo.GetByIdAsync(601));
    }

    // ─── 7. UpdateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModifiedEntity_ChangesPersisted()
    {
        var cat = MakeCategory(701, "OldName");
        await _repo.AddAsync(cat);
        await _context.SaveChangesAsync();

        cat.Name = "NewName";
        await _repo.UpdateAsync(cat);
        await _context.SaveChangesAsync();

        var updated = await _repo.GetByIdAsync(701);
        Assert.Equal("NewName", updated!.Name);
    }

    // ─── 8. DeleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntity_IsRemoved()
    {
        var cat = MakeCategory(801, "ToDelete");
        await _repo.AddAsync(cat);
        await _context.SaveChangesAsync();

        await _repo.DeleteAsync(cat);
        await _context.SaveChangesAsync();

        Assert.Null(await _repo.GetByIdAsync(801));
    }

    // ─── 9. DeleteRangeAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRangeAsync_MultipleEntities_AllRemoved()
    {
        var cats = new[] { MakeCategory(901, "R1"), MakeCategory(902, "R2") };
        await _repo.AddRangeAsync(cats);
        await _context.SaveChangesAsync();

        await _repo.DeleteRangeAsync(cats);
        await _context.SaveChangesAsync();

        Assert.Null(await _repo.GetByIdAsync(901));
        Assert.Null(await _repo.GetByIdAsync(902));
    }

    // ─── 10. AsQueryable LINQ operations ────────────────────────────────────────

    [Fact]
    public async Task AsQueryable_SupportsPagination()
    {
        var opts = new DbContextOptionsBuilder<GoldDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new GoldDbContext(opts);
        var repo = new Repository<Category>(ctx);

        for (var i = 1; i <= 10; i++)
            await repo.AddAsync(MakeCategory(i, $"Cat{i:D2}"));
        await ctx.SaveChangesAsync();

        var page2 = await repo.AsQueryable()
            .OrderBy(c => c.CategoryId)
            .Skip(5)
            .Take(5)
            .ToListAsync();

        Assert.Equal(5, page2.Count);
        Assert.Equal(6, page2[0].CategoryId);
    }

    [Fact]
    public async Task AsQueryable_SupportsProjection()
    {
        await _repo.AddAsync(MakeCategory(1001, "ProjectMe"));
        await _context.SaveChangesAsync();

        var names = await _repo.AsQueryable()
            .Where(c => c.CategoryId == 1001)
            .Select(c => c.Name)
            .ToListAsync();

        Assert.Contains("ProjectMe", names);
    }

    // ─── 11. AddRangeAsync null check ───────────────────────────────────────────

    [Fact]
    public async Task AddRangeAsync_NullEntities_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repo.AddRangeAsync(null!));
    }

    // ─── 12. DeleteAsync null check ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NullEntity_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repo.DeleteAsync(null!));
    }

    // ─── 13. UpdateAsync null check ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repo.UpdateAsync(null!));
    }

    // ─── 14. FirstOrDefaultAsync null predicate ─────────────────────────────────

    [Fact]
    public async Task FirstOrDefaultAsync_NullPredicate_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repo.FirstOrDefaultAsync(null!));
    }

    // ─── 15. GetAllAsync returns IEnumerable ────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsIEnumerable()
    {
        var result = await _repo.GetAllAsync();
        Assert.IsAssignableFrom<IEnumerable<Category>>(result);
    }
}
