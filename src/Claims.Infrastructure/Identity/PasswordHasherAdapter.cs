using Claims.Application.Abstractions.Identity;
using Claims.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Claims.Infrastructure.Identity;

/// <summary>
/// Wraps ASP.NET Core's battle-tested <see cref="PasswordHasher{T}"/> (PBKDF2)
/// behind the application's IPasswordHasher abstraction.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();
    private static readonly User Placeholder = new();

    public string Hash(string password) => _inner.HashPassword(Placeholder, password);

    public bool Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(Placeholder, hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
