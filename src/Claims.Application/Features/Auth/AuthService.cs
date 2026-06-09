using Claims.Application.Abstractions;
using Claims.Application.Abstractions.Identity;
using Claims.Application.Abstractions.Persistence;
using Claims.Application.Features.Mapping;
using Claims.Domain.Entities;
using Claims.Domain.Enums;
using Claims.Shared.Results;

namespace Claims.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;
    private readonly IDateTimeProvider _clock;

    public AuthService(
        IUserRepository users,
        ICustomerRepository customers,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService,
        IDateTimeProvider clock)
    {
        _users = users;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Self-registration only ever creates claimants, regardless of the requested role.
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, cancellationToken))
            return Error.Conflict("An account with this email already exists.", "email_taken");

        // Create the customer profile the claimant will own.
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = email,
            DateOfBirth = default,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = email
        };
        await _customers.AddAsync(customer, cancellationToken);

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Claimant,
            CustomerId = customer.Id,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = email
        };
        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        // Same generic error whether the user is missing or the password is wrong.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash, request.Password))
            return Error.Unauthorized("Invalid email or password.", "invalid_credentials");

        return BuildAuthResponse(user);
    }

    public async Task<Result<UserDto>> CreateStaffAsync(RegisterRequest request, UserRole role, CancellationToken cancellationToken = default)
    {
        if (role == UserRole.Claimant)
            return Error.Validation("Use registration to create claimant accounts.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, cancellationToken))
            return Error.Conflict("An account with this email already exists.", "email_taken");

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = role,
            CreatedAtUtc = _clock.UtcNow,
            CreatedBy = email
        };
        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    private Result<AuthResponse> BuildAuthResponse(User user)
    {
        var token = _tokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, user.ToDto());
    }
}
