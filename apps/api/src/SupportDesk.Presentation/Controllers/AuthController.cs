using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Features.Auth.GetCurrentUser;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Presentation.Auth;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Signs an agent in and returns a token. Open to everyone: it is how a token is obtained.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<LoginResponse> Login(
        LoginRequest request,
        [FromServices] LoginCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(request, ct);

    /// <summary>The signed-in agent, or 401.</summary>
    [HttpGet("me")]
    public Task<CurrentUserDto> Me([FromServices] GetCurrentUserQueryHandler handler, CancellationToken ct) =>
        handler.HandleAsync(User.AgentId(), ct);
}
