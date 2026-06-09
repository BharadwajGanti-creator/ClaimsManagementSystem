using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;

namespace Claims.Tests.Domain;

public class PolicyTests
{
    private static Policy ActivePolicy() => new()
    {
        PolicyNumber = "POL-TEST",
        Status = PolicyStatus.Active,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        CoverageLimit = 10_000m
    };

    [Fact]
    public void Active_policy_is_claimable_within_window()
    {
        ActivePolicy().IsClaimable(new DateOnly(2026, 6, 1)).Should().BeTrue();
    }

    [Fact]
    public void Cancelled_policy_is_not_claimable()
    {
        var policy = ActivePolicy();
        policy.Status = PolicyStatus.Cancelled;

        policy.IsClaimable(new DateOnly(2026, 6, 1)).Should().BeFalse();
    }

    [Fact]
    public void Incident_before_start_date_is_not_claimable()
    {
        ActivePolicy().IsClaimable(new DateOnly(2025, 12, 31)).Should().BeFalse();
    }

    [Fact]
    public void EnsureClaimable_throws_for_inactive_policy()
    {
        var policy = ActivePolicy();
        policy.Status = PolicyStatus.Expired;

        var act = () => policy.EnsureClaimable(new DateOnly(2026, 6, 1));

        act.Should().Throw<DomainException>().WithMessage("*not active*");
    }

    [Fact]
    public void EnsureClaimable_throws_for_out_of_window_incident()
    {
        var act = () => ActivePolicy().EnsureClaimable(new DateOnly(2027, 1, 1));

        act.Should().Throw<DomainException>().WithMessage("*coverage window*");
    }
}
