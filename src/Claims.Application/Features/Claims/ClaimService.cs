using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Application.Common;
using Claims.Application.Features.Mapping;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;
using Claims.Shared.Constants;
using Claims.Shared.Pagination;
using Claims.Shared.Results;

namespace Claims.Application.Features.Claims;

public sealed class ClaimService : IClaimService
{
    private readonly IClaimRepository _claims;
    private readonly IPolicyRepository _policies;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IFileStorage _fileStorage;

    public ClaimService(
        IClaimRepository claims,
        IPolicyRepository policies,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IFileStorage fileStorage)
    {
        _claims = claims;
        _policies = policies;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _fileStorage = fileStorage;
    }

    public async Task<Result<ClaimDetailDto>> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
            return Error.Unauthorized("Authentication is required.");
        if (!IsStaff && !_currentUser.IsInRole(Roles.Claimant))
            return Error.Forbidden("A recognized role is required.");
        if (_currentUser.IsInRole(Roles.Claimant) && _currentUser.CustomerId is null)
            return Error.Forbidden("A claimant must be linked to a customer.");
        if (request.PolicyId == Guid.Empty || !Enum.IsDefined(request.Type))
            return Error.Validation("A policy ID and a valid claim type are required.");
        if (request.IncidentDate == DateOnly.MinValue || request.IncidentDate > _clock.Today)
            return Error.Validation("Incident date is required and cannot be in the future.");
        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length is < 10 or > 2000)
            return Error.Validation("Description must contain 10 to 2000 characters.");
        if (request.ClaimedAmount <= 0 || request.ClaimedAmount > 9_999_999_999_999_999.99m
            || decimal.Round(request.ClaimedAmount, 2) != request.ClaimedAmount)
            return Error.Validation("Claimed amount must fit decimal(18,2), be positive and have at most two decimal places.");

        var policy = await _policies.GetWithDetailsAsync(request.PolicyId, cancellationToken);
        if (policy is null)
            return Error.NotFound($"Policy '{request.PolicyId}' was not found.");

        // A claimant may only file claims against their own policies.
        if (_currentUser.IsInRole(Roles.Claimant) && policy.CustomerId != _currentUser.CustomerId)
            return Error.Forbidden("You can only file claims on your own policies.");

        if (request.IncidentDate > _clock.Today)
            return Error.Validation("The incident date cannot be in the future.");

        try
        {
            policy.EnsureClaimable(request.IncidentDate);
        }
        catch (DomainException ex)
        {
            return Error.Conflict(ex.Message, "policy_not_claimable");
        }

        // Enforce the policy's aggregate coverage limit against the claimed amount.
        var alreadyApproved = await _claims.GetTotalApprovedAmountForPolicyAsync(policy.Id, null, cancellationToken);
        if (alreadyApproved + request.ClaimedAmount > policy.CoverageLimit)
            return Error.Conflict(
                $"Claim would exceed the policy coverage limit of {policy.CoverageLimit:C}. " +
                $"Already committed: {alreadyApproved:C}.",
                "coverage_limit_exceeded");

        var claim = new Claim
        {
            ClaimNumber = await GenerateUniqueClaimNumberAsync(cancellationToken),
            PolicyId = policy.Id,
            Type = request.Type,
            IncidentDate = request.IncidentDate,
            SubmittedAtUtc = _clock.UtcNow,
            Description = description,
            ClaimedAmount = request.ClaimedAmount,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = _currentUser.AuditName
        };
        claim.StatusHistory.Add(new ClaimStatusHistory
        {
            ClaimId = claim.Id,
            FromStatus = ClaimStatus.Submitted,
            ToStatus = ClaimStatus.Submitted,
            ChangedBy = _currentUser.AuditName,
            ChangedAtUtc = _clock.UtcNow,
            Notes = "Claim submitted."
        });

        await _claims.AddAsync(claim, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        claim.Policy = policy;
        return claim.ToDetailDto();
    }

    public async Task<Result<ClaimDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetWithDetailsAsync(id, cancellationToken);
        if (claim is null)
            return Error.NotFound($"Claim '{id}' was not found.");

        var access = EnsureCanView(claim);
        return access.IsFailure ? access.Error! : claim.ToDetailDto();
    }

    public async Task<Result<PagedResult<ClaimDto>>> GetPagedAsync(PaginationParams pagination, ClaimStatus? status, Guid? policyId, CancellationToken cancellationToken = default)
    {
        if (!IsStaff && (!_currentUser.IsInRole(Roles.Claimant) || _currentUser.CustomerId is null))
            return Error.Forbidden("A valid customer or staff identity is required.");
        // Claimants are implicitly scoped to their own customer record.
        Guid? customerScope = _currentUser.IsInRole(Roles.Claimant) ? _currentUser.CustomerId : null;

        var page = await _claims.GetPagedAsync(pagination, status, policyId, customerScope, cancellationToken);
        return new PagedResult<ClaimDto>(
            page.Items.Select(c => c.ToDto()).ToList(),
            page.TotalCount, page.PageNumber, page.PageSize);
    }

    public Task<Result<ClaimDetailDto>> StartReviewAsync(Guid id, ClaimNotesRequest request, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, (claim, actor) => claim.ChangeStatus(ClaimStatus.UnderReview, actor, request.Notes), cancellationToken);

    public Task<Result<ClaimDetailDto>> RequestInformationAsync(Guid id, ClaimNotesRequest request, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, (claim, actor) => claim.ChangeStatus(ClaimStatus.InformationRequested, actor, request.Notes), cancellationToken);

    public Task<Result<ClaimDetailDto>> RejectAsync(Guid id, RejectClaimRequest request, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, (claim, actor) => claim.Reject(actor, request.Reason), cancellationToken);

    public Task<Result<ClaimDetailDto>> CancelAsync(Guid id, ClaimNotesRequest request, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, (claim, actor) => claim.ChangeStatus(ClaimStatus.Cancelled, actor, request.Notes), cancellationToken, allowOwner: true);

    public async Task<Result<ClaimDetailDto>> ApproveAsync(Guid id, ApproveClaimRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsStaff) return Error.Forbidden("Only staff may approve claims.");
        if (request.ApprovedAmount <= 0 || decimal.Round(request.ApprovedAmount, 2) != request.ApprovedAmount)
            return Error.Validation("Approved amount must be positive and have at most two decimal places.");
        var claim = await _claims.GetWithDetailsAsync(id, cancellationToken);
        if (claim is null)
            return Error.NotFound($"Claim '{id}' was not found.");

        // Re-check the coverage limit at decision time, excluding this claim.
        var alreadyApproved = await _claims.GetTotalApprovedAmountForPolicyAsync(claim.PolicyId, claim.Id, cancellationToken);
        var limit = claim.Policy?.CoverageLimit ?? decimal.MaxValue;
        if (alreadyApproved + request.ApprovedAmount > limit)
            return Error.Conflict(
                $"Approved amount would exceed the policy coverage limit of {limit:C}.",
                "coverage_limit_exceeded");

        try
        {
            claim.Approve(request.ApprovedAmount, _currentUser.AuditName, request.Notes);
        }
        catch (DomainException ex)
        {
            return ToError(ex);
        }

        await PersistAsync(claim, cancellationToken);
        return claim.ToDetailDto();
    }

    public async Task<Result<ClaimDocumentDto>> AddDocumentAsync(Guid claimId, AddDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var claim = await _claims.GetWithDetailsAsync(claimId, cancellationToken);
        if (claim is null)
            return Error.NotFound($"Claim '{claimId}' was not found.");

        var access = EnsureCanView(claim);
        if (access.IsFailure)
            return access.Error!;

        if (claim.Status is ClaimStatus.Paid or ClaimStatus.Rejected or ClaimStatus.Cancelled)
            return Error.Conflict("Documents cannot be added to a closed claim.", "claim_closed");

        var storagePath = await _fileStorage.SaveAsync(command.FileName, command.Content, cancellationToken);

        var document = new ClaimDocument
        {
            ClaimId = claim.Id,
            FileName = command.FileName,
            ContentType = command.ContentType,
            FileSizeBytes = command.FileSizeBytes,
            StoragePath = storagePath,
            UploadedAtUtc = _clock.UtcNow,
            UploadedBy = _currentUser.AuditName
        };
        claim.Documents.Add(document);
        _claims.Update(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return document.ToDto();
    }

    /// <summary>Shared pipeline for staff-only status transitions.</summary>
    private async Task<Result<ClaimDetailDto>> TransitionAsync(
        Guid id, Action<Claim, string> transition, CancellationToken cancellationToken, bool allowOwner = false)
    {
        if (!allowOwner && !IsStaff)
            return Error.Forbidden("Only staff may adjudicate claims.");
        var claim = await _claims.GetWithDetailsAsync(id, cancellationToken);
        if (claim is null)
            return Error.NotFound($"Claim '{id}' was not found.");

        if (allowOwner)
        {
            var access = EnsureCanView(claim);
            if (access.IsFailure) return access.Error!;
        }

        try
        {
            transition(claim, _currentUser.AuditName);
        }
        catch (DomainException ex)
        {
            return ToError(ex);
        }

        await PersistAsync(claim, cancellationToken);
        return claim.ToDetailDto();
    }

    private async Task PersistAsync(Claim claim, CancellationToken cancellationToken)
    {
        claim.UpdatedAtUtc = _clock.UtcNow;
        claim.UpdatedBy = _currentUser.AuditName;
        _claims.Update(claim);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private Result EnsureCanView(Claim claim)
    {
        if (IsStaff)
            return Result.Success(); // Staff can view everything.

        return _currentUser.IsAuthenticated && _currentUser.IsInRole(Roles.Claimant)
            && _currentUser.CustomerId is { } customerId && claim.Policy?.CustomerId == customerId
            ? Result.Success()
            : Result.Failure(Error.Forbidden("You can only access your own claims."));
    }

    private bool IsStaff => _currentUser.IsAuthenticated
        && (_currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Adjuster));

    private static Error ToError(DomainException ex) =>
        ex is InvalidClaimStatusTransitionException
            ? Error.Conflict(ex.Message, "invalid_status_transition")
            : Error.Conflict(ex.Message, "domain_rule_violated");

    private async Task<string> GenerateUniqueClaimNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = ReferenceNumber.ForClaim(_clock.Today);
            if (!await _claims.ClaimNumberExistsAsync(candidate, cancellationToken))
                return candidate;
        }
        throw new InvalidOperationException("Unable to generate a unique claim number.");
    }
}
