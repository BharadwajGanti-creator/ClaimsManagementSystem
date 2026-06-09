namespace Claims.Domain.Common;

/// <summary>
/// An entity that tracks creation and modification metadata. These fields are
/// populated automatically by the persistence layer (see ClaimsDbContext.SaveChanges).
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
