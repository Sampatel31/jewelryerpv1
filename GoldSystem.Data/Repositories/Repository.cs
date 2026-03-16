using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace GoldSystem.Data.Repositories;

/// <summary>
/// Generic repository implementation backed by Entity Framework Core.
/// Supports eager loading via Include/ThenInclude through AsQueryable().
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly GoldDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public Repository(GoldDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync(new object[] { id }, cancellationToken);

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.ToListAsync(cancellationToken);

    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
        => predicate is null
            ? await DbSet.CountAsync(cancellationToken)
            : await DbSet.CountAsync(predicate, cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await DbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        DbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public virtual IQueryable<TEntity> AsQueryable()
        => DbSet.AsQueryable();
}
