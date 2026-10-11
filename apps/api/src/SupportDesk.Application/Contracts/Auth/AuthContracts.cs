namespace SupportDesk.Application.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);

/// <summary>Who is signed in: enough to greet them.</summary>
public sealed record CurrentUserDto(int Id, string FullName, string Email);

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, CurrentUserDto User);
