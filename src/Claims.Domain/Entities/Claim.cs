using Claims.Domain.Common;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;

namespace Claims.Domain.Entities;

/// <summary>
/// A request for compensation filed against a policy. The claim owns its
/// lifecycle: status changes must go through <see cref="ChangeStatus"/>, which
/// enforces the allowed workflow transitions and records an audit trail entry.
/// </summary>
public class Claim : AuditableEntity
{
    public string ClaimNumber { get; set; } = string.Empty;

    public Guid PolicyId { get; set; }
    public Policy? Policy { get; set; }

    public ClaimType Type { get; set; }
    public ClaimStatus Status { get; private set; } = ClaimStatus.Submitted;

    public DateOnly IncidentDate { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Amount requested by the claimant.</summary>
    public decimal ClaimedAmount { get; set; }

    /// <summary>Amount approved by an adjuster (null until a decision is made).</summary>
    public decimal? ApprovedAmount { get; private set; }

    public string? DecisionNotes { get; private set; }

    public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();
    public ICollection<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();
    public Payout? Payout { get; set; }

    /// <summary>
    /// Allowed state transitions for the claim workflow. Any pair not listed
    /// here is rejected.
    /// </summary>
    private static readonly IReadOnlyDictionary<ClaimStatus, ClaimStatus[]> AllowedTransitions =
        new Dictionary<ClaimStatus, ClaimStatus[]>
        {
            [ClaimStatus.Submitted] = new[] { ClaimStatus.UnderReview, ClaimStatus.Cancelled },
            [ClaimStatus.UnderReview] = new[]
            {
                ClaimStatus.InformationRequested,
                ClaimStatus.Approved,
                ClaimStatus.Rejected,
                ClaimStatus.Cancelled
            },
            [ClaimStatus.InformationRequested] = new[] { ClaimStatus.UnderReview, ClaimStatus.Cancelled },
            [ClaimStatus.Approved] = new[] { ClaimStatus.Paid },
            // Terminal states:
            [ClaimStatus.Rejected] = Array.Empty<ClaimStatus>(),
            [ClaimStatus.Paid] = Array.Empty<ClaimStatus>(),
            [ClaimStatus.Cancelled] = Array.Empty<ClaimStatus>()
        };

    public bool CanTransitionTo(ClaimStatus target) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(target);

    /// <summary>
    /// Moves the claim to <paramref name="target"/>, validating the transition
    /// and appending a history record. The caller (a service) supplies the actor.
    /// </summary>
    public void ChangeStatus(ClaimStatus target, string changedBy, string? notes = null)
    {
        if (Status == target)
            throw new DomainException($"Claim is already in status '{target}'.");

        if (!CanTransitionTo(target))
            throw new InvalidClaimStatusTransitionException(Status, target);

        var previous = Status;
        Status = target;

        StatusHistory.Add(new ClaimStatusHistory
        {
            ClaimId = Id,
            FromStatus = previous,
            ToStatus = target,
            ChangedBy = changedBy,
            ChangedAtUtc = DateTimeOffset.UtcNow,
            Notes = notes
        });
    }

    /// <summary>
    /// Approves the claim for a specific amount. The approved amount may not
    /// exceed the amount claimed.
    /// </summary>
    public void Approve(decimal approvedAmount, string changedBy, string? notes = null)
    {
        if (approvedAmount <= 0)
            throw new DomainException("Approved amount must be greater than zero.");

        if (approvedAmount > ClaimedAmount)
            throw new DomainException("Approved amount cannot exceed the claimed amount.");

        ChangeStatus(ClaimStatus.Approved, changedBy, notes);
        ApprovedAmount = approvedAmount;
        DecisionNotes = notes;
    }

    public void Reject(string changedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A rejection reason is required.");

        ChangeStatus(ClaimStatus.Rejected, changedBy, reason);
        ApprovedAmount = 0m;
        DecisionNotes = reason;
    }

    public void MarkPaid(string changedBy, string? notes = null) =>
        ChangeStatus(ClaimStatus.Paid, changedBy, notes);
}
