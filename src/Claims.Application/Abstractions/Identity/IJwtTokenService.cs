using Claims.Domain.Entities;

namespace Claims.Application.Abstractions.Identity;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessToken CreateToken(User user);
}
