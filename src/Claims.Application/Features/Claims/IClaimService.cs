using Claims.Domain.Enums;
using Claims.Shared.Pagination;
using Claims.Shared.Results;

namespace Claims.Application.Features.Claims;

public interface IClaimService
{
    Task<Result<ClaimDetailDto>> SubmitAsync(SubmitClaimRequest request, CancellationToken cancellationToken = default);
    Task<Result<ClaimDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ClaimDto>>> GetPagedAsync(PaginationParams pagination, ClaimStatus? status, Guid? policyId, CancellationToken cancellationToken = default);

    Task<Result<ClaimDetailDto>> StartReviewAsync(Guid id, ClaimNotesRequest request, CancellationToken cancellationToken = default);
    Task<Result<ClaimDetailDto>> RequestInformationAsync(Guid id, ClaimNotesRequest request, CancellationToken cancellationToken = default);
    Task<Result<ClaimDetailDto>> ApproveAsync(Guid id, ApproveClaimRequest request, CancellationToken cancellationToken = default);
    Task<Result<ClaimDetailDto>> RejectAsync(Guid id, RejectClaimRequest request, CancellationToken cancellationToken = default);
    Task<Result<ClaimDetailDto>> CancelAsync(Guid id, ClaimNotesRequest request, CancellationToken cancellationToken = default);

    Task<Result<ClaimDocumentDto>> AddDocumentAsync(Guid claimId, AddDocumentCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Carries an uploaded file's metadata + content stream into the service.</summary>
public sealed record AddDocumentCommand(string FileName, string ContentType, long FileSizeBytes, Stream Content);
