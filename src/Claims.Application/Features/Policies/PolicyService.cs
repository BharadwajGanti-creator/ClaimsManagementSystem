using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Application.Common;
using Claims.Application.Features.Mapping;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Shared.Pagination;
using Claims.Shared.Constants;
using Claims.Shared.Results;

namespace Claims.Application.Features.Policies;

public sealed class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _policies;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public PolicyService(
        IPolicyRepository policies,
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _policies = policies;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<PolicyDto>> CreateAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
            return Error.Validation("Policy end date must be after the start date.");

        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Error.NotFound($"Customer '{request.CustomerId}' was not found.");

        var policy = new Policy
        {
            PolicyNumber = await GenerateUniquePolicyNumberAsync(cancellationToken),
            CustomerId = request.CustomerId,
            Type = request.Type,
            Status = PolicyStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            CoverageLimit = request.CoverageLimit,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = _currentUser.AuditName,
            Coverages = request.Coverages.Select(c => new Coverage
            {
                Name = c.Name,
                Description = c.Description,
                CoverageAmount = c.CoverageAmount,
                Deductible = c.Deductible
            }).ToList()
        };

        await _policies.AddAsync(policy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        policy.Customer = customer;
        return policy.ToDto();
    }

    public async Task<Result<PolicyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _policies.GetWithDetailsAsync(id, cancellationToken);
        if (policy is not null && _currentUser.IsInRole(Roles.Claimant)
            && (_currentUser.CustomerId is null || policy.CustomerId != _currentUser.CustomerId))
            return Error.Forbidden("You can only access your own policies.");
        return policy is null
            ? Error.NotFound($"Policy '{id}' was not found.")
            : policy.ToDto();
    }

    public async Task<Result<PagedResult<PolicyDto>>> GetPagedAsync(PaginationParams pagination, Guid? customerId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.IsInRole(Roles.Claimant))
        {
            if (_currentUser.CustomerId is null)
                return Error.Forbidden("A claimant must be linked to a customer.");
            customerId = _currentUser.CustomerId;
        }
        var page = await _policies.GetPagedAsync(pagination, customerId, cancellationToken);
        return new PagedResult<PolicyDto>(
            page.Items.Select(p => p.ToDto()).ToList(),
            page.TotalCount, page.PageNumber, page.PageSize);
    }

    public async Task<Result<PolicyDto>> ChangeStatusAsync(Guid id, PolicyStatus status, CancellationToken cancellationToken = default)
    {
        var policy = await _policies.GetWithDetailsAsync(id, cancellationToken);
        if (policy is null)
            return Error.NotFound($"Policy '{id}' was not found.");

        policy.Status = status;
        policy.UpdatedAtUtc = _clock.UtcNow;
        policy.UpdatedBy = _currentUser.AuditName;
        _policies.Update(policy);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return policy.ToDto();
    }

    private async Task<string> GenerateUniquePolicyNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = ReferenceNumber.ForPolicy(_clock.Today);
            if (!await _policies.PolicyNumberExistsAsync(candidate, cancellationToken))
                return candidate;
        }
        throw new InvalidOperationException("Unable to generate a unique policy number.");
    }
}
