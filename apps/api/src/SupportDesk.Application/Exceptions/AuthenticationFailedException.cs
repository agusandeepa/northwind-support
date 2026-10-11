namespace SupportDesk.Application.Exceptions;

/// <summary>
/// Thrown when someone cannot be identified: a failed sign-in, or a token for an agent who is
/// gone or inactive. Surfaces as HTTP 401.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
