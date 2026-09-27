using Claims.API.Common;
using Claims.Application.Features.Auth;
using Claims.Domain.Enums;
using Claims.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Claims.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Registers a new claimant account and returns a JWT.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        (await _authService.RegisterAsync(request, ct)).ToActionResult(this);

    /// <summary>Authenticates a user and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        (await _authService.LoginAsync(request, ct)).ToActionResult(this);

    /// <summary>Admin-only: provisions an Adjuster or Admin account.</summary>
    [HttpPost("staff")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateStaff([FromQuery] UserRole role, RegisterRequest request, CancellationToken ct) =>
        (await _authService.CreateStaffAsync(request, role, ct)).ToActionResult(this);
}
