using Claims.Domain.Common;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;

namespace Claims.Domain.Entities;

/// <summary>
/// An insurance contract held by a customer, against which claims may be filed.
/// </summary>
public class Policy : AuditableEntity
{
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public PolicyType Type { get; set; }
    public PolicyStatus Status { get; set; } = PolicyStatus.Active;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Recurring premium paid by the customer.</summary>
    public decimal PremiumAmount { get; set; }

    /// <summary>Maximum aggregate amount payable across all claims on this policy.</summary>
    public decimal CoverageLimit { get; set; }

    public ICollection<Coverage> Coverages { get; set; } = new List<Coverage>();
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    /// <summary>
    /// A policy can back a new claim only when it is active and the incident
    /// falls within the coverage window.
    /// </summary>
    public bool IsClaimable(DateOnly incidentDate) =>
        Status == PolicyStatus.Active &&
        incidentDate >= StartDate &&
        incidentDate <= EndDate;

    public void EnsureClaimable(DateOnly incidentDate)
    {
        if (Status != PolicyStatus.Active)
            throw new DomainException($"Policy '{PolicyNumber}' is not active (status: {Status}).");

        if (incidentDate < StartDate || incidentDate > EndDate)
            throw new DomainException(
                $"Incident date {incidentDate:yyyy-MM-dd} is outside the policy coverage window " +
                $"({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}).");
    }
}
