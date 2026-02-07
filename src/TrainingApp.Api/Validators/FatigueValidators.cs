using FluentValidation;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Api.Validators;

public class CreateRecoveryLogRequestValidator : AbstractValidator<CreateRecoveryLogRequest>
{
    public CreateRecoveryLogRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(BeValidDate).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.SleepQuality)
            .InclusiveBetween(1, 5).WithMessage("Sleep quality must be between 1 and 5")
            .When(x => x.SleepQuality.HasValue);

        RuleFor(x => x.SleepHours)
            .InclusiveBetween(0, 24).WithMessage("Sleep hours must be between 0 and 24")
            .When(x => x.SleepHours.HasValue);

        RuleFor(x => x.StressLevel)
            .InclusiveBetween(1, 5).WithMessage("Stress level must be between 1 and 5")
            .When(x => x.StressLevel.HasValue);

        RuleFor(x => x.EnergyLevel)
            .InclusiveBetween(1, 5).WithMessage("Energy level must be between 1 and 5")
            .When(x => x.EnergyLevel.HasValue);

        RuleFor(x => x.MuscleReadiness)
            .InclusiveBetween(1, 5).WithMessage("Muscle readiness must be between 1 and 5")
            .When(x => x.MuscleReadiness.HasValue);

        RuleFor(x => x.Mood)
            .InclusiveBetween(1, 5).WithMessage("Mood must be between 1 and 5")
            .When(x => x.Mood.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string date)
        => DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}

public class RecalculateRequestValidator : AbstractValidator<RecalculateRequest>
{
    public RecalculateRequestValidator()
    {
        RuleFor(x => x.From)
            .Must(BeValidDateOrNull).WithMessage("From must be a valid date (yyyy-MM-dd)")
            .When(x => x.From is not null);
    }

    private static bool BeValidDateOrNull(string? date)
        => date is null || DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}
