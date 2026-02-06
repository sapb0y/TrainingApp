using FluentValidation;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Api.Validators;

public class CreateWorkoutRequestValidator : AbstractValidator<CreateWorkoutRequest>
{
    public CreateWorkoutRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workout name is required")
            .MaximumLength(200).WithMessage("Workout name cannot exceed 200 characters");

        RuleFor(x => x.ScheduledAt)
            .NotEmpty().WithMessage("Scheduled date is required");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes is not null);
    }
}

public class UpdateWorkoutRequestValidator : AbstractValidator<UpdateWorkoutRequest>
{
    public UpdateWorkoutRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workout name cannot be empty")
            .MaximumLength(200).WithMessage("Workout name cannot exceed 200 characters")
            .When(x => x.Name is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes is not null);

        RuleFor(x => x.SessionRpe)
            .InclusiveBetween(1, 10).WithMessage("Session RPE must be between 1 and 10")
            .When(x => x.SessionRpe.HasValue);
    }
}

public class CreateWorkoutSetRequestValidator : AbstractValidator<CreateWorkoutSetRequest>
{
    public CreateWorkoutSetRequestValidator()
    {
        RuleFor(x => x.ExerciseId)
            .NotEmpty().WithMessage("Exercise ID is required");

        RuleFor(x => x.SetNumber)
            .GreaterThan(0).WithMessage("Set number must be greater than 0");

        RuleFor(x => x.TargetReps)
            .InclusiveBetween(1, 100).WithMessage("Target reps must be between 1 and 100")
            .When(x => x.TargetReps.HasValue);

        RuleFor(x => x.TargetWeight)
            .GreaterThan(0).WithMessage("Target weight must be greater than 0")
            .When(x => x.TargetWeight.HasValue);

        RuleFor(x => x.TargetRir)
            .InclusiveBetween(0, 10).WithMessage("Target RIR must be between 0 and 10")
            .When(x => x.TargetRir.HasValue);
    }
}

public class UpdateWorkoutSetRequestValidator : AbstractValidator<UpdateWorkoutSetRequest>
{
    public UpdateWorkoutSetRequestValidator()
    {
        RuleFor(x => x.TargetReps)
            .InclusiveBetween(1, 100).WithMessage("Target reps must be between 1 and 100")
            .When(x => x.TargetReps.HasValue);

        RuleFor(x => x.TargetWeight)
            .GreaterThan(0).WithMessage("Target weight must be greater than 0")
            .When(x => x.TargetWeight.HasValue);

        RuleFor(x => x.ActualReps)
            .InclusiveBetween(1, 100).WithMessage("Actual reps must be between 1 and 100")
            .When(x => x.ActualReps.HasValue);

        RuleFor(x => x.ActualWeight)
            .GreaterThan(0).WithMessage("Actual weight must be greater than 0")
            .When(x => x.ActualWeight.HasValue);

        RuleFor(x => x.Rpe)
            .InclusiveBetween(1, 10).WithMessage("RPE must be between 1 and 10")
            .When(x => x.Rpe.HasValue);

        RuleFor(x => x.Rir)
            .InclusiveBetween(0, 10).WithMessage("RIR must be between 0 and 10")
            .When(x => x.Rir.HasValue);

        RuleFor(x => x.TargetRir)
            .InclusiveBetween(0, 10).WithMessage("Target RIR must be between 0 and 10")
            .When(x => x.TargetRir.HasValue);
    }
}

public class StartWorkoutRequestValidator : AbstractValidator<StartWorkoutRequest>
{
    public StartWorkoutRequestValidator()
    {
        RuleFor(x => x.PreWorkoutReadiness)
            .InclusiveBetween(1, 10).WithMessage("Pre-workout readiness must be between 1 and 10")
            .When(x => x.PreWorkoutReadiness.HasValue);
    }
}

public class CompleteWorkoutRequestValidator : AbstractValidator<CompleteWorkoutRequest>
{
    public CompleteWorkoutRequestValidator()
    {
        RuleFor(x => x.SessionRpe)
            .InclusiveBetween(1, 10).WithMessage("Session RPE must be between 1 and 10")
            .When(x => x.SessionRpe.HasValue);

        RuleFor(x => x.PostWorkoutFatigue)
            .InclusiveBetween(1, 10).WithMessage("Post-workout fatigue must be between 1 and 10")
            .When(x => x.PostWorkoutFatigue.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => x.Notes is not null);
    }
}
