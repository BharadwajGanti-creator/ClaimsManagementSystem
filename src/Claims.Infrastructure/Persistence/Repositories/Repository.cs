using Claims.Application.Abstractions.Persistence;
using Claims.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository. Persistence is deferred to IUnitOfWork.SaveChangesAsync.
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ClaimsDbContext Context;
    protected readonly DbSet<T> Set;

    public Repository(ClaimsDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) => Set.Update(entity);

    public virtual void Remove(T entity) => Set.Remove(entity);
}
