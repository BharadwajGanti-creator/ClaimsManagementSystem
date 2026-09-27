using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Shared.Pagination;

namespace Claims.Application.Abstractions.Persistence;

public interface IClaimRepository : IRepository<Claim>
{
    /// <summary>Loads a claim with its policy, documents, status history and payout.</summary>
    Task<Claim?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ClaimNumberExistsAsync(string claimNumber, CancellationToken cancellationToken = default);

    Task<PagedResult<Claim>> GetPagedAsync(
        PaginationParams pagination,
        ClaimStatus? status,
        Guid? policyId,
        Guid? customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of approved amounts for non-rejected/non-cancelled claims on a policy,
    /// used to enforce the policy's aggregate coverage limit.
    /// </summary>
    Task<decimal> GetTotalApprovedAmountForPolicyAsync(Guid policyId, Guid? excludeClaimId, CancellationToken cancellationToken = default);
}
