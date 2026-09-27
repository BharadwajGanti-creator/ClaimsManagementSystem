using Claims.Domain.Enums;
using Claims.Shared.Pagination;
using Claims.Shared.Results;

namespace Claims.Application.Features.Policies;

public interface IPolicyService
{
    Task<Result<PolicyDto>> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default);
    Task<Result<PolicyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<PolicyDto>>> GetPagedAsync(PaginationParams pagination, Guid? customerId, CancellationToken cancellationToken = default);
    Task<Result<PolicyDto>> ChangeStatusAsync(Guid id, PolicyStatus status, CancellationToken cancellationToken = default);
}
