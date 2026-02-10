using FluentValidation;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Api.Validators;

public class SubmitCoachApplicationRequestValidator : AbstractValidator<SubmitCoachApplicationRequest>
{
    public SubmitCoachApplicationRequestValidator()
    {
        RuleFor(x => x.Credentials)
            .NotEmpty().WithMessage("Credentials are required")
            .MaximumLength(2000);

        RuleFor(x => x.CurrentClientCount)
            .GreaterThanOrEqualTo(0).WithMessage("Client count must be non-negative");

        RuleFor(x => x.BusinessGoal)
            .NotEmpty().WithMessage("Business goal is required")
            .MaximumLength(2000);

        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(2000);
    }
}
