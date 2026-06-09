using System.ComponentModel.DataAnnotations;
using Claims.Domain.Enums;

namespace Claims.Application.Features.Claims;

public sealed record ClaimDocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset UploadedAtUtc,
    string? UploadedBy);

public sealed record ClaimStatusHistoryDto(
    ClaimStatus FromStatus,
    ClaimStatus ToStatus,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc,
    string? Notes);

public sealed record ClaimDto(
    Guid Id,
    string ClaimNumber,
    Guid PolicyId,
    string? PolicyNumber,
    ClaimType Type,
    ClaimStatus Status,
    DateOnly IncidentDate,
    DateTimeOffset SubmittedAtUtc,
    string Description,
    decimal ClaimedAmount,
    decimal? ApprovedAmount);

public sealed record ClaimDetailDto(
    Guid Id,
    string ClaimNumber,
    Guid PolicyId,
    string? PolicyNumber,
    ClaimType Type,
    ClaimStatus Status,
    DateOnly IncidentDate,
    DateTimeOffset SubmittedAtUtc,
    string Description,
    decimal ClaimedAmount,
    decimal? ApprovedAmount,
    string? DecisionNotes,
    IReadOnlyList<ClaimDocumentDto> Documents,
    IReadOnlyList<ClaimStatusHistoryDto> StatusHistory);

public sealed record SubmitClaimRequest
{
    [Required] public Guid PolicyId { get; init; }
    [Required] public ClaimType Type { get; init; }
    [Required] public DateOnly IncidentDate { get; init; }
    [Required, MinLength(10)] public string Description { get; init; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal ClaimedAmount { get; init; }
}

public sealed record ApproveClaimRequest
{
    [Range(0.01, double.MaxValue)] public decimal ApprovedAmount { get; init; }
    public string? Notes { get; init; }
}

public sealed record RejectClaimRequest
{
    [Required] public string Reason { get; init; } = string.Empty;
}

public sealed record ClaimNotesRequest
{
    public string? Notes { get; init; }
}
