using Claims.Domain.Common;

namespace Claims.Application.Abstractions.Persistence;

/// <summary>
/// Generic write/read access for an aggregate root. Specialised repositories add
/// query methods tailored to their aggregate.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
