using Claims.API.Common;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Features.Policies;
using Claims.Shared.Constants;
using Claims.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claims.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policies;
    private readonly ICurrentUser _currentUser;

    public PoliciesController(IPolicyService policies, ICurrentUser currentUser)
    {
        _policies = policies;
        _currentUser = currentUser;
    }

    /// <summary>Lists policies. Claimants only see their own.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PolicyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? customerId = null, CancellationToken ct = default)
    {
        // Force claimant scope to their own customer record.
        if (_currentUser.IsInRole(Roles.Claimant))
            customerId = _currentUser.CustomerId;

        var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
        return (await _policies.GetPagedAsync(pagination, customerId, ct)).ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _policies.GetByIdAsync(id, ct)).ToActionResult(this);

    [HttpPost]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreatePolicyRequest request, CancellationToken ct) =>
        (await _policies.CreateAsync(request, ct))
            .ToCreatedResult(this, nameof(Get), p => new { id = p.Id });

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(PolicyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(Guid id, UpdatePolicyStatusRequest request, CancellationToken ct) =>
        (await _policies.ChangeStatusAsync(id, request.Status, ct)).ToActionResult(this);
}
