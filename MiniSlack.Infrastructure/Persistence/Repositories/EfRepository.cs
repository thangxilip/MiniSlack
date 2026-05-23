using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniSlack.Application.Common.Persistence;
using MiniSlack.Domain.Common;

namespace MiniSlack.Infrastructure.Persistence.Repositories;

public class EfRepository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public EfRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query()
    {
        return _dbSet;
    }

    public IQueryable<TEntity> QueryAsNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    public async Task<TEntity> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, cancellationToken);

        return entity ?? throw new KeyNotFoundException($"{typeof(TEntity).Name} with id '{id}' was not found.");
    }

    public Task<TEntity?> FindAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return _dbSet.ToListAsync(cancellationToken);
    }

    public Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        Expression<Func<TEntity, object>>? sorting = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = sorting is null
            ? _dbSet.OrderBy(entity => entity.Id)
            : ascending
                ? _dbSet.OrderBy(sorting)
                : _dbSet.OrderByDescending(sorting);

        return query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(cancellationToken);
    }

    public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return _dbSet.LongCountAsync(cancellationToken);
    }

    public Task<long> GetCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.LongCountAsync(predicate, cancellationToken);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public async Task<TEntity> InsertAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return entity;
    }

    public async Task InsertManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<TEntity> UpdateAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return entity;
    }

    public async Task UpdateManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(
        Guid id,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(id, cancellationToken);

        await DeleteAsync(entity, autoSave, cancellationToken);
    }

    public async Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet.Where(predicate).ToListAsync(cancellationToken);

        _dbSet.RemoveRange(entities);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HardDeleteAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        _dbContext.MarkAsHardDeleted(entity);
        _dbSet.Remove(entity);

        if (autoSave)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
