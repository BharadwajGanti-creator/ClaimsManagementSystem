using System.ComponentModel.DataAnnotations;
using Claims.Domain.Enums;

namespace Claims.Application.Features.Auth;

public sealed record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Self-registration always creates a Claimant. Staff roles are assigned by
    /// an Admin via the dedicated endpoint.
    /// </summary>
    public UserRole Role { get; init; } = UserRole.Claimant;
}

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid? CustomerId);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserDto User);
