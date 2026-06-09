using System.ComponentModel.DataAnnotations;
using Claims.Domain.Enums;

namespace Claims.Application.Features.Payouts;

public sealed record PayoutDto(
    Guid Id,
    Guid ClaimId,
    decimal Amount,
    PayoutStatus Status,
    string? PaymentMethod,
    string? PaymentReference,
    DateTimeOffset? ProcessedAtUtc,
    string? FailureReason);

/// <summary>Creates a pending payout for an approved claim.</summary>
public sealed record CreatePayoutRequest
{
    [Required] public Guid ClaimId { get; init; }
    [Required] public string PaymentMethod { get; init; } = string.Empty;
}

/// <summary>Marks an existing payout as processed (settled).</summary>
public sealed record ProcessPayoutRequest
{
    [Required] public string PaymentReference { get; init; } = string.Empty;
}
