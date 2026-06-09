using Claims.Domain.Common;
using Claims.Domain.Enums;

namespace Claims.Domain.Entities;

/// <summary>
/// An authenticated principal of the system. Authentication is handled in-house
/// (hashed password) rather than via an external identity provider.
/// </summary>
public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Claimant;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// For claimants, links the login to the customer record they own. Null for
    /// staff (Admin/Adjuster).
    /// </summary>
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
