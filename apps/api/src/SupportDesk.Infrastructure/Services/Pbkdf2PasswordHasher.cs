using System.Security.Cryptography;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Infrastructure.Services;

/// <summary>
/// PBKDF2 with HMAC-SHA256 and a random salt per password, using the framework's own
/// implementation. The stored form is "v1.iterations.salt.hash", so the cost can be raised
/// later and old hashes still verify.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int DefaultIterations = 600_000;

    private const string Version = "v1";
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private static readonly byte[] DummySalt = new byte[SaltSize];

    private readonly int _iterations;

    public Pbkdf2PasswordHasher() : this(DefaultIterations)
    {
    }

    public Pbkdf2PasswordHasher(int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

        _iterations = iterations;
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Version}.{_iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string? storedHash)
    {
        if (!TryRead(storedHash, out var iterations, out var salt, out var expected))
        {
            // Do the work anyway, so a missing account is not quicker to refuse than a wrong password.
            Rfc2898DeriveBytes.Pbkdf2(password, DummySalt, _iterations, HashAlgorithmName.SHA256, HashSize);

            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryRead(string? storedHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        var parts = storedHash?.Split('.');

        if (parts is not { Length: 4 } || parts[0] != Version || !int.TryParse(parts[1], out iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length > 0 && hash.Length > 0;
    }
}
