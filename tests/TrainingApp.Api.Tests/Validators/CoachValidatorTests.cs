using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class AcceptCoachInviteRequestValidatorTests
{
    private readonly AcceptCoachInviteRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidCode()
    {
        var result = _validator.TestValidate(new AcceptCoachInviteRequest("XK3M9P"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_Empty()
    {
        var result = _validator.TestValidate(new AcceptCoachInviteRequest(""));
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_TooShort()
    {
        var result = _validator.TestValidate(new AcceptCoachInviteRequest("ABC"));
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_TooLong()
    {
        var result = _validator.TestValidate(new AcceptCoachInviteRequest("ABCDEFGH"));
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Fact]
    public void Should_HaveError_When_SpecialChars()
    {
        var result = _validator.TestValidate(new AcceptCoachInviteRequest("AB!@#D"));
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }
}

public class UpdatePermissionsRequestValidatorTests
{
    private readonly UpdatePermissionsRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_OnePermissionSet()
    {
        var result = _validator.TestValidate(new UpdatePermissionsRequest(true, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_NotHaveError_When_AllPermissionsSet()
    {
        var result = _validator.TestValidate(new UpdatePermissionsRequest(true, false, true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_AllNull()
    {
        var result = _validator.TestValidate(new UpdatePermissionsRequest(null, null, null));
        result.ShouldHaveAnyValidationError();
    }
}

public class CoachNoteRequestValidatorTests
{
    private readonly CoachNoteRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_Valid()
    {
        var result = _validator.TestValidate(new CoachNoteRequest("Good session!", null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_Empty()
    {
        var result = _validator.TestValidate(new CoachNoteRequest("", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_HaveError_When_TooLong()
    {
        var result = _validator.TestValidate(new CoachNoteRequest(new string('x', 501), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }
}

public class CoachModifySetRequestValidatorTests
{
    private readonly CoachModifySetRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_Valid()
    {
        var result = _validator.TestValidate(new CoachModifySetRequest(8, 100m, 8.0m));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_NegativeReps()
    {
        var result = _validator.TestValidate(new CoachModifySetRequest(-1, null, null));
        result.ShouldHaveValidationErrorFor(x => x.TargetReps);
    }

    [Fact]
    public void Should_HaveError_When_RpeOutOfRange()
    {
        var result = _validator.TestValidate(new CoachModifySetRequest(null, null, 11m));
        result.ShouldHaveValidationErrorFor(x => x.TargetRpe);
    }
}
