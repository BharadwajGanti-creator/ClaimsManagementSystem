using Claims.API.Common;
using Claims.Application.Features.Claims;
using Claims.Domain.Enums;
using Claims.Shared.Constants;
using Claims.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claims.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClaimsController : ControllerBase
{
    private const long MaxDocumentBytes = 10 * 1024 * 1024; // 10 MB
    private readonly IClaimService _claims;

    public ClaimsController(IClaimService claims) => _claims = claims;

    /// <summary>Files a new claim. Claimants may only file on their own policies.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit(SubmitClaimRequest request, CancellationToken ct) =>
        (await _claims.SubmitAsync(request, ct))
            .ToCreatedResult(this, nameof(Get), c => new { id = c.Id });

    /// <summary>Lists claims. Claimants are scoped to their own claims automatically.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ClaimStatus? status = null, [FromQuery] Guid? policyId = null, CancellationToken ct = default)
    {
        var pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
        return (await _claims.GetPagedAsync(pagination, status, policyId, ct)).ToActionResult(this);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClaimDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        (await _claims.GetByIdAsync(id, ct)).ToActionResult(this);

    // --- Adjudication workflow (staff only) ---

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> StartReview(Guid id, ClaimNotesRequest request, CancellationToken ct) =>
        (await _claims.StartReviewAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/request-info")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> RequestInformation(Guid id, ClaimNotesRequest request, CancellationToken ct) =>
        (await _claims.RequestInformationAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Approve(Guid id, ApproveClaimRequest request, CancellationToken ct) =>
        (await _claims.ApproveAsync(id, request, ct)).ToActionResult(this);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Reject(Guid id, RejectClaimRequest request, CancellationToken ct) =>
        (await _claims.RejectAsync(id, request, ct)).ToActionResult(this);

    /// <summary>Cancels a claim. Allowed for staff and the owning claimant.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, ClaimNotesRequest request, CancellationToken ct) =>
        (await _claims.CancelAsync(id, request, ct)).ToActionResult(this);

    // --- Documents ---

    [HttpPost("{id:guid}/documents")]
    [ProducesResponseType(typeof(ClaimDocumentDto), StatusCodes.Status200OK)]
    [RequestSizeLimit(MaxDocumentBytes)]
    public async Task<IActionResult> UploadDocument(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProblemDetails { Title = "no_file", Detail = "A non-empty file is required." });

        if (file.Length > MaxDocumentBytes)
            return BadRequest(new ProblemDetails { Title = "file_too_large", Detail = "Maximum file size is 10 MB." });

        await using var stream = file.OpenReadStream();
        var command = new AddDocumentCommand(file.FileName, file.ContentType, file.Length, stream);
        return (await _claims.AddDocumentAsync(id, command, ct)).ToActionResult(this);
    }
}
