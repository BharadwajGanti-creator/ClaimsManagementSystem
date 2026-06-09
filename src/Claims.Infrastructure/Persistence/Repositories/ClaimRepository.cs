using Claims.Application.Abstractions.Persistence;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence.Repositories;

public sealed class ClaimRepository : Repository<Claim>, IClaimRepository
{
    public ClaimRepository(ClaimsDbContext context) : base(context) { }

    public async Task<Claim?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set
            .Include(c => c.Policy)
            .Include(c => c.Documents)
            .Include(c => c.StatusHistory)
            .Include(c => c.Payout)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<bool> ClaimNumberExistsAsync(string claimNumber, CancellationToken cancellationToken = default) =>
        await Set.AnyAsync(c => c.ClaimNumber == claimNumber, cancellationToken);

    public async Task<PagedResult<Claim>> GetPagedAsync(
        PaginationParams pagination,
        ClaimStatus? status,
        Guid? policyId,
        Guid? customerId,
        CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(c => c.Policy).AsQueryable();

        if (status is { } s)
            query = query.Where(c => c.Status == s);

        if (policyId is { } pid)
            query = query.Where(c => c.PolicyId == pid);

        if (customerId is { } cid)
            query = query.Where(c => c.Policy!.CustomerId == cid);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.SubmittedAtUtc)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Claim>(items, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<decimal> GetTotalApprovedAmountForPolicyAsync(Guid policyId, Guid? excludeClaimId, CancellationToken cancellationToken = default)
    {
        // Count amounts committed by claims that are approved or already paid.
        var query = Set.AsNoTracking().Where(c =>
            c.PolicyId == policyId &&
            (c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Paid));

        if (excludeClaimId is { } id)
            query = query.Where(c => c.Id != id);

        // Sum of a nullable decimal returns 0 when there are no rows.
        return await query.SumAsync(c => c.ApprovedAmount ?? 0m, cancellationToken);
    }
}
