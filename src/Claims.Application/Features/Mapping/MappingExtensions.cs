using Claims.Application.Features.Auth;
using Claims.Application.Features.Claims;
using Claims.Application.Features.Customers;
using Claims.Application.Features.Payouts;
using Claims.Application.Features.Policies;
using Claims.Domain.Entities;

namespace Claims.Application.Features.Mapping;

/// <summary>
/// Hand-written entity-to-DTO projections. Kept explicit (no AutoMapper) so the
/// application layer carries no extra dependencies and mappings stay obvious.
/// </summary>
public static class MappingExtensions
{
    public static UserDto ToDto(this User user) => new(
        user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.CustomerId);

    public static CustomerDto ToDto(this Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Email, c.PhoneNumber, c.DateOfBirth,
        c.AddressLine1, c.AddressLine2, c.City, c.PostalCode, c.Country, c.CreatedAtUtc);

    public static CoverageDto ToDto(this Coverage c) => new(
        c.Id, c.Name, c.Description, c.CoverageAmount, c.Deductible);

    public static PolicyDto ToDto(this Policy p) => new(
        p.Id, p.PolicyNumber, p.CustomerId, p.Customer?.FullName, p.Type, p.Status,
        p.StartDate, p.EndDate, p.PremiumAmount, p.CoverageLimit,
        p.Coverages.Select(c => c.ToDto()).ToList());

    public static ClaimDto ToDto(this Claim c) => new(
        c.Id, c.ClaimNumber, c.PolicyId, c.Policy?.PolicyNumber, c.Type, c.Status,
        c.IncidentDate, c.SubmittedAtUtc, c.Description, c.ClaimedAmount, c.ApprovedAmount);

    public static ClaimDocumentDto ToDto(this ClaimDocument d) => new(
        d.Id, d.FileName, d.ContentType, d.FileSizeBytes, d.UploadedAtUtc, d.UploadedBy);

    public static ClaimStatusHistoryDto ToDto(this ClaimStatusHistory h) => new(
        h.FromStatus, h.ToStatus, h.ChangedBy, h.ChangedAtUtc, h.Notes);

    public static ClaimDetailDto ToDetailDto(this Claim c) => new(
        c.Id, c.ClaimNumber, c.PolicyId, c.Policy?.PolicyNumber, c.Type, c.Status,
        c.IncidentDate, c.SubmittedAtUtc, c.Description, c.ClaimedAmount, c.ApprovedAmount,
        c.DecisionNotes,
        c.Documents.Select(d => d.ToDto()).ToList(),
        c.StatusHistory.OrderBy(h => h.ChangedAtUtc).Select(h => h.ToDto()).ToList());

    public static PayoutDto ToDto(this Payout p) => new(
        p.Id, p.ClaimId, p.Amount, p.Status, p.PaymentMethod, p.PaymentReference,
        p.ProcessedAtUtc, p.FailureReason);
}
