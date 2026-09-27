namespace Claims.Domain.Enums;

/// <summary>
/// Coarse-grained authorization roles. String names are mirrored in
/// Claims.Shared.Constants.Roles for use in [Authorize] attributes.
/// </summary>
public enum UserRole
{
    /// <summary>Full administrative access.</summary>
    Admin = 1,

    /// <summary>Reviews and decides on claims.</summary>
    Adjuster = 2,

    /// <summary>End customer who submits and tracks their own claims.</summary>
    Claimant = 3
}
