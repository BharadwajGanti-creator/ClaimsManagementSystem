using System.ComponentModel.DataAnnotations;
using Claims.Domain.Enums;

namespace Claims.Application.Features.Policies;

public sealed record CoverageDto(
    Guid Id,
    string Name,
    string? Description,
    decimal CoverageAmount,
    decimal Deductible);

public sealed record PolicyDto(
    Guid Id,
    string PolicyNumber,
    Guid CustomerId,
    string? CustomerName,
    PolicyType Type,
    PolicyStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal PremiumAmount,
    decimal CoverageLimit,
    IReadOnlyList<CoverageDto> Coverages);

public sealed record CreateCoverageRequest
{
    [Required] public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    [Range(0, double.MaxValue)] public decimal CoverageAmount { get; init; }
    [Range(0, double.MaxValue)] public decimal Deductible { get; init; }
}

public sealed record CreatePolicyRequest
{
    [Required] public Guid CustomerId { get; init; }
    [Required] public PolicyType Type { get; init; }
    [Required] public DateOnly StartDate { get; init; }
    [Required] public DateOnly EndDate { get; init; }
    [Range(0, double.MaxValue)] public decimal PremiumAmount { get; init; }
    [Range(0, double.MaxValue)] public decimal CoverageLimit { get; init; }
    public IReadOnlyList<CreateCoverageRequest> Coverages { get; init; } = new List<CreateCoverageRequest>();
}

public sealed record UpdatePolicyStatusRequest
{
    [Required] public PolicyStatus Status { get; init; }
}
