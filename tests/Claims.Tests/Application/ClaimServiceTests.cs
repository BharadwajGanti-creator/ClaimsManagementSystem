using Claims.Application.Features.Claims;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Shared.Constants;
using Claims.Shared.Results;
using Claims.Tests.Fakes;

namespace Claims.Tests.Application;

public class ClaimServiceTests
{
    private static readonly DateOnly Today = new(2026, 6, 9);

    private static Policy ActivePolicy(decimal coverageLimit = 10_000m, Guid? customerId = null) => new()
    {
        Id = Guid.NewGuid(),
        PolicyNumber = "POL-1",
        CustomerId = customerId ?? Guid.NewGuid(),
        Status = PolicyStatus.Active,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        CoverageLimit = coverageLimit
    };

    private static ClaimService BuildService(
        InMemoryClaimRepository claims, InMemoryPolicyRepository policies, FakeCurrentUser user) =>
        new(claims, policies, new FakeUnitOfWork(), user, new FixedClock(Today), new NoOpFileStorage());

    private static SubmitClaimRequest Request(Guid policyId, decimal amount = 1000m) => new()
    {
        PolicyId = policyId,
        Type = ClaimType.Accident,
        IncidentDate = new DateOnly(2026, 5, 1),
        Description = "A valid description of the incident.",
        ClaimedAmount = amount
    };

    [Fact]
    public async Task Submit_succeeds_for_valid_claim()
    {
        var policy = ActivePolicy();
        var claims = new InMemoryClaimRepository();
        var service = BuildService(claims, new InMemoryPolicyRepository(policy), new FakeCurrentUser());

        var result = await service.SubmitAsync(Request(policy.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ClaimStatus.Submitted);
        claims.Store.Should().ContainSingle();
    }

    [Fact]
    public async Task Submit_fails_when_policy_missing()
    {
        var service = BuildService(new InMemoryClaimRepository(), new InMemoryPolicyRepository(), new FakeCurrentUser());

        var result = await service.SubmitAsync(Request(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Submit_fails_for_future_incident_date()
    {
        var policy = ActivePolicy();
        var service = BuildService(new InMemoryClaimRepository(), new InMemoryPolicyRepository(policy), new FakeCurrentUser());

        var request = Request(policy.Id) with { IncidentDate = Today.AddDays(5) };
        var result = await service.SubmitAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Submit_enforces_policy_coverage_limit()
    {
        var policy = ActivePolicy(coverageLimit: 1000m);
        var claims = new InMemoryClaimRepository();
        // An already-approved claim consuming the whole limit.
        var existing = new Claim { PolicyId = policy.Id, ClaimedAmount = 1000m, Description = "x", ClaimNumber = "C0" };
        existing.ChangeStatus(ClaimStatus.UnderReview, "a");
        existing.Approve(1000m, "a");
        claims.Store[existing.Id] = existing;

        var service = BuildService(claims, new InMemoryPolicyRepository(policy), new FakeCurrentUser());

        var result = await service.SubmitAsync(Request(policy.Id, amount: 500m));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("coverage_limit_exceeded");
    }

    [Fact]
    public async Task Claimant_cannot_submit_against_another_customers_policy()
    {
        var policy = ActivePolicy(customerId: Guid.NewGuid());
        var claimant = new FakeCurrentUser { Role = Roles.Claimant, CustomerId = Guid.NewGuid() };
        var service = BuildService(new InMemoryClaimRepository(), new InMemoryPolicyRepository(policy), claimant);

        var result = await service.SubmitAsync(Request(policy.Id));

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Approve_then_reject_is_blocked_by_workflow()
    {
        var policy = ActivePolicy();
        var claims = new InMemoryClaimRepository();
        var service = BuildService(claims, new InMemoryPolicyRepository(policy), new FakeCurrentUser());

        var submitted = await service.SubmitAsync(Request(policy.Id, 2000m));
        var id = submitted.Value.Id;

        await service.StartReviewAsync(id, new ClaimNotesRequest());
        var approved = await service.ApproveAsync(id, new ApproveClaimRequest { ApprovedAmount = 1500m });
        approved.IsSuccess.Should().BeTrue();

        var reject = await service.RejectAsync(id, new RejectClaimRequest { Reason = "changed mind" });

        reject.IsFailure.Should().BeTrue();
        reject.Error!.Code.Should().Be("invalid_status_transition");
    }
}
