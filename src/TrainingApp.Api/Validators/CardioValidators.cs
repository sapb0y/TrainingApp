using FluentValidation;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Validators;

public class LogCardioRequestValidator : AbstractValidator<LogCardioRequest>
{
    private static readonly string[] ValidModalities = Enum.GetNames<CardioModality>();
    private static readonly string[] ValidZones = Enum.GetNames<CardioIntensityZone>();

    public LogCardioRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(BeValidDate).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.Modality)
            .NotEmpty().WithMessage("Modality is required")
            .Must(m => ValidModalities.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Modality must be one of: {string.Join(", ", ValidModalities)}");

        RuleFor(x => x.Zone)
            .NotEmpty().WithMessage("Zone is required")
            .Must(z => ValidZones.Contains(z, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Zone must be one of: {string.Join(", ", ValidZones)}");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, 600).WithMessage("Duration must be between 1 and 600 minutes");

        RuleFor(x => x.DistanceKm)
            .InclusiveBetween(0.01m, 500m).WithMessage("Distance must be between 0.01 and 500 km")
            .When(x => x.DistanceKm.HasValue);

        RuleFor(x => x.AverageHeartRate)
            .InclusiveBetween(30, 250).WithMessage("Average heart rate must be between 30 and 250 bpm")
            .When(x => x.AverageHeartRate.HasValue);

        RuleFor(x => x.MaxHeartRate)
            .InclusiveBetween(30, 250).WithMessage("Max heart rate must be between 30 and 250 bpm")
            .When(x => x.MaxHeartRate.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string date)
        => DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}
