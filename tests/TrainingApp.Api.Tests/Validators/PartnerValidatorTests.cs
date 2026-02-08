using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class AcceptInviteRequestValidatorTests
{
    private readonly AcceptInviteRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidCode()
    {
        var req = new AcceptInviteRequest("XK3M9P");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_Empty()
    {
        var req = new AcceptInviteRequest("");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_TooShort()
    {
        var req = new AcceptInviteRequest("ABC");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_TooLong()
    {
        var req = new AcceptInviteRequest("ABCDEFGH");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_SpecialChars()
    {
        var req = new AcceptInviteRequest("AB!@#D");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }
}

public class DeclineInviteRequestValidatorTests
{
    private readonly DeclineInviteRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidCode()
    {
        var req = new DeclineInviteRequest("XK3M9P");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_Empty()
    {
        var req = new DeclineInviteRequest("");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_SpecialChars()
    {
        var req = new DeclineInviteRequest("AB-CD!");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }
}
