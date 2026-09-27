using Claims.API.Common;
using Claims.Application.Features.Payouts;
using Claims.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claims.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Staff)]
public sealed class PayoutsController : ControllerBase
{
    private readonly IPayoutService _payouts;

    public PayoutsController(IPayoutService payouts) => _payouts = payouts;

    /// <summary>Creates a pending payout for an approved claim.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(CreatePayoutRequest request, CancellationToken ct) =>
        (await _payouts.CreateAsync(request, ct)).ToActionResult(this);

    /// <summary>Marks a payout as processed and moves the claim to Paid.</summary>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Process(Guid id, ProcessPayoutRequest request, CancellationToken ct) =>
        (await _payouts.ProcessAsync(id, request, ct)).ToActionResult(this);

    [HttpGet("by-claim/{claimId:guid}")]
    [ProducesResponseType(typeof(PayoutDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByClaim(Guid claimId, CancellationToken ct) =>
        (await _payouts.GetByClaimIdAsync(claimId, ct)).ToActionResult(this);
}
