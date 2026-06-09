namespace Claims.Application.Abstractions.Identity;

/// <summary>
/// Ambient information about the authenticated caller, surfaced from the JWT.
/// Implemented in the API layer over IHttpContextAccessor.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }

    /// <summary>Customer linked to the caller, when the caller is a claimant.</summary>
    Guid? CustomerId { get; }

    bool IsInRole(string role);

    /// <summary>A stable identifier for audit fields (email, falling back to "system").</summary>
    string AuditName { get; }
}
