using System.Security.Claims;
using Claims.Application.Abstractions.Identity;

namespace Claims.API.Security;

/// <summary>
/// Surfaces the authenticated caller from the JWT carried on the current request.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public Guid? CustomerId =>
        Guid.TryParse(Principal?.FindFirstValue("customer_id"), out var id) ? id : null;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public string AuditName => Email ?? "system";
}
