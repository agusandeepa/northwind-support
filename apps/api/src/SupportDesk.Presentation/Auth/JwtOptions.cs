using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SupportDesk.Presentation.Auth;

/// <summary>
/// How tokens are issued and checked. The signing key comes from configuration (the
/// Jwt__Key environment variable), never from code.
/// </summary>
public sealed class JwtOptions
{
    public const int MinimumKeyBytes = 32;

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required SymmetricSecurityKey SigningKey { get; init; }

    public required TimeSpan Lifetime { get; init; }

    /// <summary>True when no key was configured and a temporary one was made for this run (Development only).</summary>
    public bool KeyWasGenerated { get; init; }

    /// <exception cref="InvalidOperationException">The key is missing outside Development, or too short.</exception>
    public static JwtOptions Create(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Jwt");
        var key = section["Key"];
        var generated = false;

        if (string.IsNullOrWhiteSpace(key))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Jwt:Key is not set. Provide it through configuration or the Jwt__Key environment variable.");
            }

            key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            generated = true;
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);

        if (keyBytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException($"Jwt:Key must be at least {MinimumKeyBytes} bytes long.");
        }

        return new JwtOptions
        {
            Issuer = section["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not set."),
            Audience = section["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not set."),
            Lifetime = TimeSpan.FromMinutes(
                section.GetValue<double?>("LifetimeMinutes")
                ?? throw new InvalidOperationException("Jwt:LifetimeMinutes is not set.")),
            SigningKey = new SymmetricSecurityKey(keyBytes),
            KeyWasGenerated = generated
        };
    }
}
