using System.Linq.Expressions;
using MiniSlack.Domain.Common;

namespace MiniSlack.Application.Common.Persistence;

public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
    where TEntity : BaseEntity
{
    Task<TEntity> InsertAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task InsertManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task UpdateManyAsync(
        IEnumerable<TEntity> entities,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool autoSave = false,
        CancellationToken cancellationToken = default);

    Task HardDeleteAsync(
        TEntity entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default);
}
