using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Application.Features.Mapping;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;
using Claims.Shared.Results;

namespace Claims.Application.Features.Payouts;

public sealed class PayoutService : IPayoutService
{
    private readonly IClaimRepository _claims;
    private readonly IRepository<Payout> _payouts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PayoutService(
        IClaimRepository claims,
        IRepository<Payout> payouts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _claims = claims;
        _payouts = payouts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<PayoutDto>> CreateAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetWithDetailsAsync(request.ClaimId, cancellationToken);
        if (claim is null)
            return Error.NotFound($"Claim '{request.ClaimId}' was not found.");

        if (claim.Status != ClaimStatus.Approved)
            return Error.Conflict("A payout can only be created for an approved claim.", "claim_not_approved");

        if (claim.Payout is not null)
            return Error.Conflict("A payout already exists for this claim.", "payout_exists");

        var payout = new Payout
        {
            ClaimId = claim.Id,
            Amount = claim.ApprovedAmount ?? 0m,
            PaymentMethod = request.PaymentMethod,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = _currentUser.AuditName
        };

        await _payouts.AddAsync(payout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return payout.ToDto();
    }

    public async Task<Result<PayoutDto>> ProcessAsync(Guid payoutId, ProcessPayoutRequest request, CancellationToken cancellationToken = default)
    {
        var payout = await _payouts.GetByIdAsync(payoutId, cancellationToken);
        if (payout is null)
            return Error.NotFound($"Payout '{payoutId}' was not found.");

        try
        {
            payout.MarkProcessed(request.PaymentReference);
        }
        catch (DomainException ex)
        {
            return Error.Conflict(ex.Message, "payout_already_processed");
        }

        payout.UpdatedAtUtc = _clock.UtcNow;
        payout.UpdatedBy = _currentUser.AuditName;
        _payouts.Update(payout);

        // Move the claim to its terminal Paid state.
        var claim = await _claims.GetWithDetailsAsync(payout.ClaimId, cancellationToken);
        if (claim is not null && claim.Status == ClaimStatus.Approved)
        {
            claim.MarkPaid(_currentUser.AuditName, $"Payout {payout.PaymentReference} processed.");
            _claims.Update(claim);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return payout.ToDto();
    }

    public async Task<Result<PayoutDto>> GetByClaimIdAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetWithDetailsAsync(claimId, cancellationToken);
        if (claim?.Payout is null)
            return Error.NotFound($"No payout found for claim '{claimId}'.");

        return claim.Payout.ToDto();
    }
}
