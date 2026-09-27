using Claims.Domain.Enums;
using Claims.Shared.Results;

namespace Claims.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Admin-only: provisions a staff account (Adjuster/Admin).</summary>
    Task<Result<UserDto>> CreateStaffAsync(RegisterRequest request, UserRole role, CancellationToken cancellationToken = default);
}
