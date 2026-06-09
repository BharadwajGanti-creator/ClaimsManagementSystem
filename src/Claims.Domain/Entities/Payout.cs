using Claims.Domain.Common;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;

namespace Claims.Domain.Entities;

/// <summary>
/// A disbursement made to settle an approved claim. A claim has at most one payout.
/// </summary>
public class Payout : AuditableEntity
{
    public Guid ClaimId { get; set; }
    public Claim? Claim { get; set; }

    public decimal Amount { get; set; }
    public PayoutStatus Status { get; private set; } = PayoutStatus.Pending;

    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkProcessed(string paymentReference)
    {
        if (Status == PayoutStatus.Processed)
            throw new DomainException("Payout has already been processed.");

        Status = PayoutStatus.Processed;
        PaymentReference = paymentReference;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
        FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        Status = PayoutStatus.Failed;
        FailureReason = reason;
    }
}
