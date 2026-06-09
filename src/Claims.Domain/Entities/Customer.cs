using Claims.Domain.Common;

namespace Claims.Domain.Entities;

/// <summary>
/// A person insured by one or more policies.
/// </summary>
public class Customer : AuditableEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
