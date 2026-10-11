namespace SupportDesk.Application.Abstractions;

/// <summary>Turns passwords into hashes that are safe to store, and checks a password against one.</summary>
public interface IPasswordHasher
{
    /// <summary>A salted, slow hash of the password. Never reversible.</summary>
    string Hash(string password);

    /// <summary>
    /// Whether the password matches the stored hash. A missing or unreadable hash never
    /// matches, but costs the same time as a real check, so the speed of a refusal does not
    /// tell whether an account exists.
    /// </summary>
    bool Verify(string password, string? storedHash);
}
