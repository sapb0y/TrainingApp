using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class AdminValidatorTests
{
    private readonly ChangeRoleRequestValidator _roleValidator = new();
    private readonly OverrideTierRequestValidator _tierValidator = new();
    private readonly ExtendTrialRequestValidator _trialValidator = new();
    private readonly AdminCancelRequestValidator _cancelValidator = new();

    [Theory]
    [InlineData("Admin")]
    [InlineData("Coach")]
    [InlineData("Athlete")]
    public void ChangeRole_ValidRole_Passes(string role)
    {
        var result = _roleValidator.TestValidate(new ChangeRoleRequest(role));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("SuperAdmin")]
    [InlineData("User")]
    public void ChangeRole_InvalidRole_Fails(string role)
    {
        var result = _roleValidator.TestValidate(new ChangeRoleRequest(role));
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void OverrideTier_Valid_Passes()
    {
        var result = _tierValidator.TestValidate(new OverrideTierRequest("Competitor", "Courtesy upgrade"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OverrideTier_InvalidTier_Fails()
    {
        var result = _tierValidator.TestValidate(new OverrideTierRequest("Gold", "Reason"));
        result.ShouldHaveValidationErrorFor(x => x.Tier);
    }

    [Fact]
    public void OverrideTier_MissingReason_Fails()
    {
        var result = _tierValidator.TestValidate(new OverrideTierRequest("Athlete", ""));
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void ExtendTrial_ValidDays_Passes()
    {
        var result = _trialValidator.TestValidate(new ExtendTrialRequest(14));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ExtendTrial_ZeroDays_Fails()
    {
        var result = _trialValidator.TestValidate(new ExtendTrialRequest(0));
        result.ShouldHaveValidationErrorFor(x => x.Days);
    }

    [Fact]
    public void ExtendTrial_Over90Days_Fails()
    {
        var result = _trialValidator.TestValidate(new ExtendTrialRequest(91));
        result.ShouldHaveValidationErrorFor(x => x.Days);
    }

    [Fact]
    public void AdminCancel_ValidReason_Passes()
    {
        var result = _cancelValidator.TestValidate(new AdminCancelRequest("Abuse detected"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AdminCancel_EmptyReason_Fails()
    {
        var result = _cancelValidator.TestValidate(new AdminCancelRequest(""));
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
