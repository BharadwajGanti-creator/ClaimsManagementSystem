using Claims.Domain.Enums;

namespace Claims.Domain.Exceptions;

/// <summary>
/// Thrown when an attempt is made to move a claim between two states that the
/// workflow does not permit.
/// </summary>
public sealed class InvalidClaimStatusTransitionException : DomainException
{
    public InvalidClaimStatusTransitionException(ClaimStatus from, ClaimStatus to)
        : base($"A claim cannot transition from '{from}' to '{to}'.")
    {
        From = from;
        To = to;
    }

    public ClaimStatus From { get; }
    public ClaimStatus To { get; }
}
