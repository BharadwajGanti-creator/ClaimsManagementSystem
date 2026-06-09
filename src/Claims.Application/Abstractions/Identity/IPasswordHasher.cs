namespace Claims.Application.Abstractions.Identity;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Returns true when <paramref name="password"/> matches <paramref name="hash"/>.</summary>
    bool Verify(string hash, string password);
}
