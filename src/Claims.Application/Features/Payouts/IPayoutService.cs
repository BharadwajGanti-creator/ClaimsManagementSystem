using Claims.Shared.Results;

namespace Claims.Application.Features.Payouts;

public interface IPayoutService
{
    Task<Result<PayoutDto>> CreateAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);
    Task<Result<PayoutDto>> ProcessAsync(Guid payoutId, ProcessPayoutRequest request, CancellationToken cancellationToken = default);
    Task<Result<PayoutDto>> GetByClaimIdAsync(Guid claimId, CancellationToken cancellationToken = default);
}
