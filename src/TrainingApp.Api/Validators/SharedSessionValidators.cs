using FluentValidation;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Api.Validators;

public class CreateSharedSessionRequestValidator : AbstractValidator<CreateSharedSessionRequest>
{
    public CreateSharedSessionRequestValidator()
    {
        RuleFor(x => x.PartnershipId)
            .NotEmpty().WithMessage("Partnership ID is required");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .Must(BeValidDate).WithMessage("Date must be a valid date (yyyy-MM-dd)");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters")
            .When(x => x.Notes is not null);
    }

    private static bool BeValidDate(string? date)
        => date is not null && DateOnly.TryParseExact(date, "yyyy-MM-dd", out _);
}
