namespace Claims.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Uses a GUID surrogate key so that
/// identifiers can be generated client-side without a database round-trip.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
