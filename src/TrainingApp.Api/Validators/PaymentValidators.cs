using FluentValidation;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Validators;

public class CreateCheckoutRequestValidator : AbstractValidator<CreateCheckoutRequest>
{
    public CreateCheckoutRequestValidator()
    {
        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier is required")
            .Must(t => Enum.TryParse<SubscriptionTier>(t, true, out _))
            .WithMessage("Invalid tier. Must be Athlete, Competitor, or Coach");

        RuleFor(x => x.Interval)
            .NotEmpty().WithMessage("Interval is required")
            .Must(i => Enum.TryParse<BillingInterval>(i, true, out _))
            .WithMessage("Invalid interval. Must be Monthly or Annual");

        RuleFor(x => x.SuccessUrl)
            .NotEmpty().WithMessage("SuccessUrl is required")
            .Must(BeValidAbsoluteUrl).WithMessage("SuccessUrl must be a valid absolute URL");

        RuleFor(x => x.CancelUrl)
            .NotEmpty().WithMessage("CancelUrl is required")
            .Must(BeValidAbsoluteUrl).WithMessage("CancelUrl must be a valid absolute URL");
    }

    private static bool BeValidAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https");
}

public class CreatePortalRequestValidator : AbstractValidator<CreatePortalRequest>
{
    public CreatePortalRequestValidator()
    {
        RuleFor(x => x.ReturnUrl)
            .NotEmpty().WithMessage("ReturnUrl is required")
            .Must(BeValidAbsoluteUrl).WithMessage("ReturnUrl must be a valid absolute URL");
    }

    private static bool BeValidAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https");
}
