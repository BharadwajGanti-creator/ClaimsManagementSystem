using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Domain.Exceptions;

namespace Claims.Tests.Domain;

public class ClaimWorkflowTests
{
    private static Claim NewClaim(decimal claimed = 1000m) => new()
    {
        ClaimNumber = "CLM-TEST",
        PolicyId = Guid.NewGuid(),
        Type = ClaimType.Accident,
        IncidentDate = new DateOnly(2026, 1, 1),
        Description = "Test incident description",
        ClaimedAmount = claimed
    };

    [Fact]
    public void New_claim_starts_in_Submitted()
    {
        NewClaim().Status.Should().Be(ClaimStatus.Submitted);
    }

    [Fact]
    public void Submitted_can_move_to_UnderReview()
    {
        var claim = NewClaim();

        claim.ChangeStatus(ClaimStatus.UnderReview, "adjuster@x");

        claim.Status.Should().Be(ClaimStatus.UnderReview);
        claim.StatusHistory.Should().ContainSingle()
            .Which.ToStatus.Should().Be(ClaimStatus.UnderReview);
    }

    [Fact]
    public void Submitted_cannot_jump_to_Approved()
    {
        var claim = NewClaim();

        var act = () => claim.Approve(500m, "adjuster@x");

        act.Should().Throw<InvalidClaimStatusTransitionException>();
    }

    [Fact]
    public void Approve_cannot_exceed_claimed_amount()
    {
        var claim = NewClaim(claimed: 1000m);
        claim.ChangeStatus(ClaimStatus.UnderReview, "a");

        var act = () => claim.Approve(1500m, "a");

        act.Should().Throw<DomainException>().WithMessage("*cannot exceed*");
    }

    [Fact]
    public void Full_happy_path_Submitted_to_Paid()
    {
        var claim = NewClaim(2000m);

        claim.ChangeStatus(ClaimStatus.UnderReview, "a");
        claim.Approve(1500m, "a", "Looks valid");
        claim.MarkPaid("a");

        claim.Status.Should().Be(ClaimStatus.Paid);
        claim.ApprovedAmount.Should().Be(1500m);
        claim.StatusHistory.Should().HaveCount(3);
    }

    [Fact]
    public void Rejected_is_terminal()
    {
        var claim = NewClaim();
        claim.ChangeStatus(ClaimStatus.UnderReview, "a");
        claim.Reject("a", "Insufficient evidence");

        var act = () => claim.ChangeStatus(ClaimStatus.UnderReview, "a");

        act.Should().Throw<InvalidClaimStatusTransitionException>();
        claim.ApprovedAmount.Should().Be(0m);
    }

    [Fact]
    public void Reject_requires_a_reason()
    {
        var claim = NewClaim();
        claim.ChangeStatus(ClaimStatus.UnderReview, "a");

        var act = () => claim.Reject("a", "  ");

        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    [Theory]
    [InlineData(ClaimStatus.InformationRequested)]
    [InlineData(ClaimStatus.Approved)]
    [InlineData(ClaimStatus.Rejected)]
    public void UnderReview_can_transition_to_review_outcomes(ClaimStatus target)
    {
        var claim = NewClaim();
        claim.ChangeStatus(ClaimStatus.UnderReview, "a");

        claim.CanTransitionTo(target).Should().BeTrue();
    }

    [Fact]
    public void Changing_to_same_status_throws()
    {
        var claim = NewClaim();

        var act = () => claim.ChangeStatus(ClaimStatus.Submitted, "a");

        act.Should().Throw<DomainException>().WithMessage("*already*");
    }
}
