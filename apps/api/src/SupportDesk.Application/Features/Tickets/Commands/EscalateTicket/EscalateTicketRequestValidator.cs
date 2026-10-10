using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public const int MinReasonLength = 5;
    public const int MaxReasonLength = 500;
    public const int MaxActorLength = 100;

    public EscalateTicketRequestValidator()
    {
        // Length is measured after trimming, which is what gets stored.
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(reason => reason.Trim().Length >= MinReasonLength)
            .WithMessage($"The reason must be at least {MinReasonLength} characters.")
            .Must(reason => reason.Trim().Length <= MaxReasonLength)
            .WithMessage($"The reason must be at most {MaxReasonLength} characters.");

        RuleFor(x => x.EscalatedBy)
            .NotEmpty()
            .MaximumLength(MaxActorLength);
    }
}
