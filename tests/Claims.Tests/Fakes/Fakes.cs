using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Shared.Constants;
using Claims.Shared.Pagination;

namespace Claims.Tests.Fakes;

/// <summary>Deterministic clock for tests.</summary>
public sealed class FixedClock : IDateTimeProvider
{
    public FixedClock(DateOnly today) => Today = today;
    public DateTimeOffset UtcNow => new(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    public DateOnly Today { get; }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;
    public Guid? UserId { get; init; } = Guid.NewGuid();
    public string? Email { get; init; } = "tester@x";
    public string? Role { get; init; } = Roles.Adjuster;
    public Guid? CustomerId { get; init; }
    public bool IsInRole(string role) => string.Equals(role, Role, StringComparison.OrdinalIgnoreCase);
    public string AuditName => Email ?? "system";
}

public sealed class NoOpFileStorage : IFileStorage
{
    public Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default) =>
        Task.FromResult($"stored/{fileName}");
    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(null);
    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class InMemoryPolicyRepository : IPolicyRepository
{
    private readonly Dictionary<Guid, Policy> _store = new();
    public InMemoryPolicyRepository(params Policy[] seed)
    {
        foreach (var p in seed) _store[p.Id] = p;
    }

    public Task<Policy?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));
    public Task<Policy?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.GetValueOrDefault(id));
    public Task<bool> PolicyNumberExistsAsync(string policyNumber, CancellationToken ct = default) =>
        Task.FromResult(_store.Values.Any(p => p.PolicyNumber == policyNumber));
    public Task<PagedResult<Policy>> GetPagedAsync(PaginationParams pagination, Guid? customerId, CancellationToken ct = default)
    {
        var items = _store.Values.Where(p => customerId == null || p.CustomerId == customerId).ToList();
        return Task.FromResult(new PagedResult<Policy>(items, items.Count, pagination.PageNumber, pagination.PageSize));
    }
    public Task AddAsync(Policy entity, CancellationToken ct = default) { _store[entity.Id] = entity; return Task.CompletedTask; }
    public void Update(Policy entity) => _store[entity.Id] = entity;
    public void Remove(Policy entity) => _store.Remove(entity.Id);
}

public sealed class InMemoryClaimRepository : IClaimRepository
{
    public readonly Dictionary<Guid, Claim> Store = new();

    public Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Store.GetValueOrDefault(id));
    public Task<Claim?> GetWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Store.GetValueOrDefault(id));
    public Task<bool> ClaimNumberExistsAsync(string claimNumber, CancellationToken ct = default) =>
        Task.FromResult(Store.Values.Any(c => c.ClaimNumber == claimNumber));

    public Task<PagedResult<Claim>> GetPagedAsync(PaginationParams pagination, ClaimStatus? status,
        Guid? policyId, Guid? customerId, CancellationToken ct = default)
    {
        var items = Store.Values
            .Where(c => status == null || c.Status == status)
            .Where(c => policyId == null || c.PolicyId == policyId)
            .ToList();
        return Task.FromResult(new PagedResult<Claim>(items, items.Count, pagination.PageNumber, pagination.PageSize));
    }

    public Task<decimal> GetTotalApprovedAmountForPolicyAsync(Guid policyId, Guid? excludeClaimId, CancellationToken ct = default)
    {
        var total = Store.Values
            .Where(c => c.PolicyId == policyId
                        && (c.Status == ClaimStatus.Approved || c.Status == ClaimStatus.Paid)
                        && (excludeClaimId == null || c.Id != excludeClaimId))
            .Sum(c => c.ApprovedAmount ?? 0m);
        return Task.FromResult(total);
    }

    public Task AddAsync(Claim entity, CancellationToken ct = default) { Store[entity.Id] = entity; return Task.CompletedTask; }
    public void Update(Claim entity) => Store[entity.Id] = entity;
    public void Remove(Claim entity) => Store.Remove(entity.Id);
}
