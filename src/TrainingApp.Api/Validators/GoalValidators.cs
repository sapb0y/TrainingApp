using FluentValidation;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Validators;

public class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
{
    private static readonly string[] ValidTypes = Enum.GetNames<GoalType>();

    public CreateGoalRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .Must(t => ValidTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Type must be one of: {string.Join(", ", ValidTypes)}");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than 0")
            .When(x => x.TargetValue.HasValue);

        RuleFor(x => x.TargetDate)
            .Must(BeValidDate).WithMessage("Target date must be a valid date (yyyy-MM-dd)")
            .When(x => x.TargetDate is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string? date)
        => date is null || DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}

public class UpdateGoalRequestValidator : AbstractValidator<UpdateGoalRequest>
{
    private static readonly string[] ValidStatuses = Enum.GetNames<GoalStatus>();

    public UpdateGoalRequestValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}")
            .When(x => x.Status is not null);

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than 0")
            .When(x => x.TargetValue.HasValue);

        RuleFor(x => x.TargetDate)
            .Must(BeValidDate).WithMessage("Target date must be a valid date (yyyy-MM-dd)")
            .When(x => x.TargetDate is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string? date)
        => date is null || DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}

public class AddCheckpointRequestValidator : AbstractValidator<AddCheckpointRequest>
{
    public AddCheckpointRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(BeValidDate).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required");
    }

    private static bool BeValidDate(string date)
        => DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}
