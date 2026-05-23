using System.Linq.Expressions;
using MiniSlack.Domain.Common;

namespace MiniSlack.Application.Common.Persistence;

public interface IReadOnlyRepository<TEntity>
    where TEntity : BaseEntity
{
    IQueryable<TEntity> Query();

    IQueryable<TEntity> QueryAsNoTracking();

    Task<TEntity> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FindAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        Expression<Func<TEntity, object>>? sorting = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}
