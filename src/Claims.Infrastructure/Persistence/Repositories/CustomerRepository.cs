using Claims.Application.Abstractions.Persistence;
using Claims.Domain.Entities;
using Claims.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ClaimsDbContext context) : base(context) { }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await Set.AnyAsync(c => c.Email == email && (excludeId == null || c.Id != excludeId), cancellationToken);

    public async Task<PagedResult<Customer>> GetPagedAsync(PaginationParams pagination, string? search, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.FirstName.Contains(term) ||
                c.LastName.Contains(term) ||
                c.Email.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Customer>(items, total, pagination.PageNumber, pagination.PageSize);
    }
}
