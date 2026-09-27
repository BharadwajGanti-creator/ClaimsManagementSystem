using Claims.Domain.Entities;
using Claims.Shared.Pagination;

namespace Claims.Application.Abstractions.Persistence;

public interface IPolicyRepository : IRepository<Policy>
{
    Task<Policy?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> PolicyNumberExistsAsync(string policyNumber, CancellationToken cancellationToken = default);
    Task<PagedResult<Policy>> GetPagedAsync(PaginationParams pagination, Guid? customerId, CancellationToken cancellationToken = default);
}
