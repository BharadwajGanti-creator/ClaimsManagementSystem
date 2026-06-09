using Claims.Domain.Common;

namespace Claims.Domain.Entities;

/// <summary>
/// Metadata for a file (photo, report, invoice) attached to a claim. The binary
/// content lives in blob storage / the file system; only the location is stored here.
/// </summary>
public class ClaimDocument : BaseEntity
{
    public Guid ClaimId { get; set; }
    public Claim? Claim { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>Relative storage key/path where the binary content is persisted.</summary>
    public string StoragePath { get; set; } = string.Empty;

    public DateTimeOffset UploadedAtUtc { get; set; }
    public string? UploadedBy { get; set; }
}
