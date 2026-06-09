using Claims.Domain.Entities;
using Claims.Shared.Pagination;

namespace Claims.Application.Abstractions.Persistence;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Customer>> GetPagedAsync(PaginationParams pagination, string? search, CancellationToken cancellationToken = default);
}
