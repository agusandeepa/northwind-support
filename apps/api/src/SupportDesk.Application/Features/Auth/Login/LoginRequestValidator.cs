using FluentValidation;
using SupportDesk.Application.Contracts.Auth;

namespace SupportDesk.Application.Features.Auth.Login;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);

        // Capped so that nobody can make the server hash an enormous password.
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
