using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Application.Features.Mapping;
using Claims.Domain.Entities;
using Claims.Shared.Pagination;
using Claims.Shared.Results;

namespace Claims.Application.Features.Customers;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CustomerService(
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _customers.EmailExistsAsync(email, null, cancellationToken))
            return Error.Conflict("A customer with this email already exists.", "email_taken");

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = _currentUser.AuditName
        };

        await _customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            return Error.NotFound($"Customer '{id}' was not found.");

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.PhoneNumber = request.PhoneNumber;
        customer.AddressLine1 = request.AddressLine1;
        customer.AddressLine2 = request.AddressLine2;
        customer.City = request.City;
        customer.PostalCode = request.PostalCode;
        customer.Country = request.Country;
        customer.UpdatedAtUtc = _clock.UtcNow;
        customer.UpdatedBy = _currentUser.AuditName;

        _customers.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.ToDto();
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        return customer is null
            ? Error.NotFound($"Customer '{id}' was not found.")
            : customer.ToDto();
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetPagedAsync(PaginationParams pagination, string? search, CancellationToken cancellationToken = default)
    {
        var page = await _customers.GetPagedAsync(pagination, search, cancellationToken);
        var dto = new PagedResult<CustomerDto>(
            page.Items.Select(c => c.ToDto()).ToList(),
            page.TotalCount, page.PageNumber, page.PageSize);
        return dto;
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            return Result.Failure(Error.NotFound($"Customer '{id}' was not found."));

        _customers.Remove(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
