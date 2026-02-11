using FluentAssertions;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class PaymentValidatorTests
{
    private readonly CreateCheckoutRequestValidator _checkoutValidator = new();
    private readonly CreatePortalRequestValidator _portalValidator = new();

    [Fact]
    public void Checkout_ValidRequest_Passes()
    {
        var request = new CreateCheckoutRequest("Competitor", "Monthly", "https://example.com/success", "https://example.com/cancel");
        var result = _checkoutValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Checkout_InvalidTier_Fails()
    {
        var request = new CreateCheckoutRequest("SuperHero", "Monthly", "https://example.com/success", "https://example.com/cancel");
        var result = _checkoutValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tier");
    }

    [Fact]
    public void Checkout_InvalidInterval_Fails()
    {
        var request = new CreateCheckoutRequest("Competitor", "Weekly", "https://example.com/success", "https://example.com/cancel");
        var result = _checkoutValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Interval");
    }

    [Fact]
    public void Checkout_InvalidSuccessUrl_Fails()
    {
        var request = new CreateCheckoutRequest("Competitor", "Monthly", "not-a-url", "https://example.com/cancel");
        var result = _checkoutValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SuccessUrl");
    }

    [Fact]
    public void Checkout_InvalidCancelUrl_Fails()
    {
        var request = new CreateCheckoutRequest("Competitor", "Monthly", "https://example.com/success", "ftp://bad.com");
        var result = _checkoutValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CancelUrl");
    }

    [Fact]
    public void Portal_ValidRequest_Passes()
    {
        var request = new CreatePortalRequest("https://example.com/dashboard");
        var result = _portalValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Portal_InvalidReturnUrl_Fails()
    {
        var request = new CreatePortalRequest("not-a-url");
        var result = _portalValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnUrl");
    }
}
