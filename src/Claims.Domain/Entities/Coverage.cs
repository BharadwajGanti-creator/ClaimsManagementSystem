using Claims.Domain.Common;

namespace Claims.Domain.Entities;

/// <summary>
/// A specific peril or benefit covered by a policy, with its own sub-limit and
/// deductible.
/// </summary>
public class Coverage : BaseEntity
{
    public Guid PolicyId { get; set; }
    public Policy? Policy { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Maximum amount payable for this individual coverage.</summary>
    public decimal CoverageAmount { get; set; }

    /// <summary>Amount the claimant must bear before this coverage pays out.</summary>
    public decimal Deductible { get; set; }
}
