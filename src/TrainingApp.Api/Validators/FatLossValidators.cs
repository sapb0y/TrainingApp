using FluentValidation;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Validators;

public class LogWeightRequestValidator : AbstractValidator<LogWeightRequest>
{
    public LogWeightRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(BeValidDate).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.WeightKg)
            .InclusiveBetween(20m, 500m).WithMessage("Weight must be between 20 and 500 kg");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string date)
        => DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}

public class StartDeficitRequestValidator : AbstractValidator<StartDeficitRequest>
{
    private static readonly string[] ValidStrategies = Enum.GetNames<DeficitStrategy>();

    public StartDeficitRequestValidator()
    {
        RuleFor(x => x.StartWeightKg)
            .InclusiveBetween(20m, 500m).WithMessage("Start weight must be between 20 and 500 kg");

        RuleFor(x => x.TargetWeightKg)
            .InclusiveBetween(20m, 500m).WithMessage("Target weight must be between 20 and 500 kg")
            .When(x => x.TargetWeightKg.HasValue);

        RuleFor(x => x.WeeklyRateKg)
            .InclusiveBetween(0.1m, 2.0m).WithMessage("Weekly rate must be between 0.1 and 2.0 kg");

        RuleFor(x => x.Strategy)
            .NotEmpty().WithMessage("Strategy is required")
            .Must(s => ValidStrategies.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Strategy must be one of: {string.Join(", ", ValidStrategies)}");

        RuleFor(x => x.DietBreakIntervalWeeks)
            .InclusiveBetween(1, 52).WithMessage("Diet break interval must be between 1 and 52 weeks")
            .When(x => x.DietBreakIntervalWeeks.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes is not null);
    }
}

public class LogNeatRequestValidator : AbstractValidator<LogNeatRequest>
{
    public LogNeatRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(BeValidDate).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.StepCount)
            .InclusiveBetween(0, 200000).WithMessage("Step count must be between 0 and 200,000");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string date)
        => DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}
