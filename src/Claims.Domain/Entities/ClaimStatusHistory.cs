using Claims.Domain.Common;
using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

/// <summary>
/// An immutable audit record of a single claim status transition. Written by
/// <see cref="Claim.ChangeStatus"/>.
/// </summary>
public class ClaimStatusHistory : BaseEntity
{
    public Guid ClaimId { get; set; }
    public Claim? Claim { get; set; }

    public ClaimStatus FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }

    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; }
    public string? Notes { get; set; }
}
