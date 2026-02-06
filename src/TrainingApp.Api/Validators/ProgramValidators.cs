using FluentValidation;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Validators;

public class GenerateProgramRequestValidator : AbstractValidator<GenerateProgramRequest>
{
    public GenerateProgramRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Program name is required")
            .MaximumLength(200).WithMessage("Program name cannot exceed 200 characters");

        RuleFor(x => x.Goal)
            .NotEmpty().WithMessage("Goal is required")
            .Must(g => Enum.TryParse<ProgramGoal>(g, true, out _))
            .WithMessage("Invalid goal. Valid values: Hypertrophy, Strength, PowerBuilding, GeneralFitness");

        RuleFor(x => x.Template)
            .NotEmpty().WithMessage("Template is required")
            .Must(t => Enum.TryParse<ProgramTemplate>(t, true, out _))
            .WithMessage("Invalid template. Valid values: PushPullLegs, UpperLower, FullBody, BroSplit");

        RuleFor(x => x.DurationWeeks)
            .InclusiveBetween(4, 52).WithMessage("Duration must be between 4 and 52 weeks");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required")
            .Must(s => DateOnly.TryParse(s, out _))
            .WithMessage("Invalid date format");
    }
}

public class UpdateProgramRequestValidator : AbstractValidator<UpdateProgramRequest>
{
    public UpdateProgramRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Program name cannot be empty")
            .MaximumLength(200).WithMessage("Program name cannot exceed 200 characters")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters")
            .When(x => x.Description is not null);

        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<ProgramStatus>(s, true, out _))
            .WithMessage("Invalid status. Valid values: Draft, Active, Completed, Archived")
            .When(x => x.Status is not null);
    }
}
