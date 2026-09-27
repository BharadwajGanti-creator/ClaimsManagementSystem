namespace Claims.Shared.Constants;

/// <summary>
/// String role names used in JWT claims and [Authorize(Roles = ...)] attributes.
/// These mirror the Claims.Domain.Enums.UserRole values.
/// </summary>
public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string Adjuster = nameof(Adjuster);
    public const string Claimant = nameof(Claimant);

    /// <summary>Roles permitted to make adjudication decisions.</summary>
    public const string Staff = Admin + "," + Adjuster;
}
