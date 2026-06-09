namespace Claims.Domain.Enums;

/// <summary>
/// Lifecycle states for a claim. Allowed transitions are enforced by
/// <see cref="Claims.Domain.Entities.Claim"/> rather than by callers.
/// </summary>
public enum ClaimStatus
{
    Submitted = 1,
    UnderReview = 2,
    InformationRequested = 3,
    Approved = 4,
    Rejected = 5,
    Paid = 6,
    Cancelled = 7
}
