using FluentValidation;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Api.Validators;

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("Invite code is required")
            .Length(6).WithMessage("Invite code must be 6 characters")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Invite code must be alphanumeric");
    }
}

public class DeclineInviteRequestValidator : AbstractValidator<DeclineInviteRequest>
{
    public DeclineInviteRequestValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("Invite code is required")
            .Length(6).WithMessage("Invite code must be 6 characters")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Invite code must be alphanumeric");
    }
}
