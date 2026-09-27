using Claims.Application.Abstractions.Persistence;
using Claims.Domain.Entities;
using Claims.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence.Repositories;

public sealed class PolicyRepository : Repository<Policy>, IPolicyRepository
{
    public PolicyRepository(ClaimsDbContext context) : base(context) { }

    public async Task<Policy?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set
            .Include(p => p.Customer)
            .Include(p => p.Coverages)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> PolicyNumberExistsAsync(string policyNumber, CancellationToken cancellationToken = default) =>
        await Set.AnyAsync(p => p.PolicyNumber == policyNumber, cancellationToken);

    public async Task<PagedResult<Policy>> GetPagedAsync(PaginationParams pagination, Guid? customerId, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(p => p.Customer).Include(p => p.Coverages).AsQueryable();

        if (customerId is { } cid)
            query = query.Where(p => p.CustomerId == cid);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.StartDate)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Policy>(items, total, pagination.PageNumber, pagination.PageSize);
    }
}
