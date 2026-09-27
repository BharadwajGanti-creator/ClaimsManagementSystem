using System.Net;
using System.Net.Http.Json;
using Claims.Application.Features.Auth;
using Claims.Application.Features.Claims;
using Claims.Application.Features.Policies;
using Claims.Application.Features.Payouts;
using Claims.Domain.Enums;
using Claims.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Claims.Tests.Integration;

public sealed class ClaimsApiTests : IAsyncLifetime
{
    private readonly string path = Path.Combine(AppContext.BaseDirectory, $"aligned-claims-{Guid.NewGuid():N}.db");
    private ClaimsApiFactory factory = null!;
    private HttpClient owner = null!;
    private HttpClient otherOwner = null!;
    private HttpClient staff = null!;
    private Guid policyId;
    private Guid customerId;

    public async Task InitializeAsync()
    {
        await using (var database = ClaimsApiFactory.Database(path)) await database.Database.MigrateAsync();
        factory = new ClaimsApiFactory(path);
        staff = factory.Client(ClaimsApiFactory.Token());
        using var anonymous = factory.Client();
        var alice = await RegisterAsync(anonymous, "alice@example.test");
        var bob = await RegisterAsync(anonymous, "bob@example.test");
        owner = factory.Client(alice.AccessToken);
        otherOwner = factory.Client(bob.AccessToken);
        customerId = alice.User.CustomerId!.Value;
        using var created = await staff.PostAsJsonAsync("/api/policies", new CreatePolicyRequest
        {
            CustomerId = customerId, Type = PolicyType.Auto,
            StartDate = new DateOnly(2020, 1, 1), EndDate = new DateOnly(2035, 1, 1),
            CoverageLimit = 100_000m, PremiumAmount = 250m
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        policyId = (await created.Content.ReadFromJsonAsync<PolicyDto>())!.Id;
    }

    public async Task DisposeAsync()
    {
        owner.Dispose(); otherOwner.Dispose(); staff.Dispose();
        await factory.DisposeAsync();
        foreach (var suffix in new[] { "", "-shm", "-wal" }) File.Delete(path + suffix);
    }

    private SubmitClaimRequest Valid => new()
    {
        PolicyId = policyId, Type = ClaimType.Accident,
        IncidentDate = new DateOnly(2026, 6, 1), Description = " A valid incident description. ", ClaimedAmount = 1234.56m
    };

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email, Password = "TestPassword#12345", FirstName = "Test", LastName = "Claimant",
            Role = UserRole.Admin // Public registration must ignore attempted privilege escalation.
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal("Claimant", result.User.Role);
        return result;
    }

    private async Task<ClaimDetailDto> SubmitAsync()
    {
        using var response = await owner.PostAsJsonAsync("/api/claims", Valid);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ClaimDetailDto>())!;
        Assert.EndsWith($"/api/Claims/{result.Id}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        return result;
    }

    [Fact]
    public async Task SubmissionPersistsClaimAndHistoryAndSurvivesRestart()
    {
        var claim = await SubmitAsync();
        Assert.Equal(1234.56m, claim.ClaimedAmount);
        Assert.Equal("A valid incident description.", claim.Description);
        Assert.Equal(ClaimStatus.Submitted, claim.Status);
        Assert.Single(claim.StatusHistory);
        await using (var database = ClaimsApiFactory.Database(path))
        {
            Assert.Equal(1, await database.Claims.CountAsync());
            Assert.Equal(1, await database.ClaimStatusHistory.CountAsync());
        }
        await factory.DisposeAsync();
        factory = new ClaimsApiFactory(path);
        using var restarted = factory.Client(ClaimsApiFactory.Token("Claimant", customerId));
        var retrieved = await restarted.GetFromJsonAsync<ClaimDetailDto>($"/api/claims/{claim.Id}");
        Assert.Equal(claim.Id, retrieved!.Id);
        Assert.Equal(claim.ClaimedAmount, retrieved.ClaimedAmount);
        Assert.Single(retrieved.StatusHistory);
        await using var reopened = ClaimsApiFactory.Database(path);
        await reopened.Database.MigrateAsync();
        Assert.Equal(1, await reopened.Claims.CountAsync());
    }

    [Fact]
    public async Task CrossCustomerReadCancelAndPolicyAccessAreDeniedWithoutChangingState()
    {
        var claim = await SubmitAsync();
        using var get = await otherOwner.GetAsync($"/api/claims/{claim.Id}");
        using var cancel = await otherOwner.PostAsJsonAsync($"/api/claims/{claim.Id}/cancel", new ClaimNotesRequest());
        using var policy = await otherOwner.GetAsync($"/api/policies/{policyId}");
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, policy.StatusCode);
        Assert.Equal(ClaimStatus.Submitted,
            (await owner.GetFromJsonAsync<ClaimDetailDto>($"/api/claims/{claim.Id}"))!.Status);
        var list = await otherOwner.GetFromJsonAsync<PagedResult<ClaimDto>>("/api/claims");
        Assert.Empty(list!.Items);
    }

    [Fact]
    public async Task OwnerCanCancelButCannotAdjudicate()
    {
        var claim = await SubmitAsync();
        using var review = await owner.PostAsJsonAsync($"/api/claims/{claim.Id}/review", new ClaimNotesRequest());
        Assert.Equal(HttpStatusCode.Forbidden, review.StatusCode);
        using var cancel = await owner.PostAsJsonAsync($"/api/claims/{claim.Id}/cancel", new ClaimNotesRequest());
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(ClaimStatus.Cancelled, (await cancel.Content.ReadFromJsonAsync<ClaimDetailDto>())!.Status);
    }

    [Fact]
    public async Task ReviewApprovalAndPayoutPreserveTheExistingWorkflow()
    {
        var claim = await SubmitAsync();
        using var review = await staff.PostAsJsonAsync($"/api/claims/{claim.Id}/review", new ClaimNotesRequest());
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        using var approval = await staff.PostAsJsonAsync($"/api/claims/{claim.Id}/approve",
            new ApproveClaimRequest { ApprovedAmount = 1000.25m });
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        using var created = await staff.PostAsJsonAsync("/api/payouts",
            new CreatePayoutRequest { ClaimId = claim.Id, PaymentMethod = "BankTransfer" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var payout = (await created.Content.ReadFromJsonAsync<PayoutDto>())!;
        using var processed = await staff.PostAsJsonAsync($"/api/payouts/{payout.Id}/process",
            new ProcessPayoutRequest { PaymentReference = "TEST-PAYMENT-123" });
        Assert.Equal(HttpStatusCode.OK, processed.StatusCode);
        var paid = (await owner.GetFromJsonAsync<ClaimDetailDto>($"/api/claims/{claim.Id}"))!;
        Assert.Equal(ClaimStatus.Paid, paid.Status);
        Assert.Equal(4, paid.StatusHistory.Count);
        var list = await owner.GetFromJsonAsync<PagedResult<ClaimDto>>("/api/claims");
        Assert.Single(list!.Items);
        // Exercises decimal SUM with an approved/paid claim on SQLite.
        using var next = await owner.PostAsJsonAsync("/api/claims", Valid);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
    }

    [Fact]
    public async Task DocumentUploadPersistsNewChildAndChecksOwnership()
    {
        var claim = await SubmitAsync();
        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent("test document"u8.ToArray()), "file", "evidence.txt");
        using var denied = await otherOwner.PostAsync($"/api/claims/{claim.Id}/documents", upload);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var accepted = await owner.PostAsync($"/api/claims/{claim.Id}/documents", upload);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        await using var database = ClaimsApiFactory.Database(path);
        Assert.Equal(1, await database.ClaimDocuments.CountAsync());
        var loaded = (await owner.GetFromJsonAsync<ClaimDetailDto>($"/api/claims/{claim.Id}"))!;
        Assert.Single(loaded.Documents);
        Assert.Single(loaded.StatusHistory);
    }

    [Fact]
    public async Task RegistrationLoginAndReadinessWork()
    {
        using var anonymous = factory.Client();
        using var login = await anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "alice@example.test", Password = "TestPassword#12345" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var badLogin = await anonymous.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "alice@example.test", Password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);
        using var readiness = await anonymous.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
    }

    [Theory]
    [InlineData("amount-zero")]
    [InlineData("amount-negative")]
    [InlineData("amount-precision")]
    [InlineData("amount-overflow")]
    [InlineData("missing-date")]
    [InlineData("future-date")]
    [InlineData("missing-policy")]
    [InlineData("invalid-type")]
    [InlineData("blank-description")]
    [InlineData("long-description")]
    public async Task InvalidSubmissionsWriteNeitherClaimNorHistory(string failure)
    {
        var request = failure switch
        {
            "amount-zero" => Valid with { ClaimedAmount = 0 },
            "amount-negative" => Valid with { ClaimedAmount = -1 },
            "amount-precision" => Valid with { ClaimedAmount = 10.001m },
            "amount-overflow" => Valid with { ClaimedAmount = decimal.MaxValue },
            "missing-date" => Valid with { IncidentDate = default },
            "future-date" => Valid with { IncidentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) },
            "missing-policy" => Valid with { PolicyId = Guid.Empty },
            "invalid-type" => Valid with { Type = (ClaimType)1234 },
            "blank-description" => Valid with { Description = "                  " },
            "long-description" => Valid with { Description = new string('a', 2001) },
            _ => throw new InvalidOperationException()
        };
        using var response = await owner.PostAsJsonAsync("/api/claims", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var database = ClaimsApiFactory.Database(path);
        Assert.Equal(0, await database.Claims.CountAsync());
        Assert.Equal(0, await database.ClaimStatusHistory.CountAsync());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("expiration")]
    [InlineData("unknown-role")]
    [InlineData("unlinked-claimant")]
    public async Task InvalidCredentialsCannotAccessTheClaimsApi(string failure)
    {
        var token = failure switch
        {
            "missing" => null,
            "signature" => ClaimsApiFactory.Token(key: new string('x', 64)),
            "issuer" => ClaimsApiFactory.Token(issuer: "untrusted"),
            "audience" => ClaimsApiFactory.Token(audience: "other-api"),
            "expired" => ClaimsApiFactory.Token(expired: true),
            "expiration" => ClaimsApiFactory.Token(includeExpiration: false),
            "unknown-role" => ClaimsApiFactory.Token(role: "Unknown"),
            "unlinked-claimant" => ClaimsApiFactory.Token(role: "Claimant"),
            _ => throw new InvalidOperationException()
        };
        using var client = factory.Client(token);
        using var get = await client.GetAsync("/api/claims");
        using var post = await client.PostAsJsonAsync("/api/claims", Valid);
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
        Assert.Contains(post.Headers.WwwAuthenticate, value => value.Scheme == "Bearer");
    }
}
