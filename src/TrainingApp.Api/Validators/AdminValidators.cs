using FluentValidation;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Validators;

public class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(r => r is "Admin" or "Coach" or "Athlete")
            .WithMessage("Role must be Admin, Coach, or Athlete");
    }
}

public class OverrideTierRequestValidator : AbstractValidator<OverrideTierRequest>
{
    public OverrideTierRequestValidator()
    {
        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier is required")
            .Must(t => Enum.TryParse<SubscriptionTier>(t, true, out _))
            .WithMessage("Invalid tier. Valid values: Athlete, Competitor, Coach");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}

public class ExtendTrialRequestValidator : AbstractValidator<ExtendTrialRequest>
{
    public ExtendTrialRequestValidator()
    {
        RuleFor(x => x.Days)
            .GreaterThan(0).WithMessage("Days must be greater than 0")
            .LessThanOrEqualTo(90).WithMessage("Days cannot exceed 90");
    }
}

public class AdminCancelRequestValidator : AbstractValidator<AdminCancelRequest>
{
    public AdminCancelRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");
    }
}
