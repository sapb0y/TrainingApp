using FluentValidation;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Api.Validators;

public class AcceptCoachInviteRequestValidator : AbstractValidator<AcceptCoachInviteRequest>
{
    public AcceptCoachInviteRequestValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("Invite code is required")
            .Length(6).WithMessage("Invite code must be 6 characters")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Invite code must be alphanumeric");
    }
}

public class UpdatePermissionsRequestValidator : AbstractValidator<UpdatePermissionsRequest>
{
    public UpdatePermissionsRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.CanModifyProgram.HasValue || x.CanViewMetrics.HasValue || x.CanAddNotes.HasValue)
            .WithMessage("At least one permission must be specified");
    }
}

public class CoachNoteRequestValidator : AbstractValidator<CoachNoteRequest>
{
    public CoachNoteRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Note content is required")
            .MaximumLength(500).WithMessage("Note content must be 500 characters or less");
    }
}

public class CoachModifySetRequestValidator : AbstractValidator<CoachModifySetRequest>
{
    public CoachModifySetRequestValidator()
    {
        RuleFor(x => x.TargetReps).GreaterThan(0).When(x => x.TargetReps.HasValue)
            .WithMessage("Target reps must be positive");
        RuleFor(x => x.TargetWeight).GreaterThan(0).When(x => x.TargetWeight.HasValue)
            .WithMessage("Target weight must be positive");
        RuleFor(x => x.TargetRpe).InclusiveBetween(1, 10).When(x => x.TargetRpe.HasValue)
            .WithMessage("Target RPE must be between 1 and 10");
    }
}
