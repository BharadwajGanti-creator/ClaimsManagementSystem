namespace Claims.Application.Abstractions.Persistence;

/// <summary>
/// Commits all pending changes across repositories in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
