using FluentAssertions;
using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class GenerateProgramRequestValidatorTests
{
    private readonly GenerateProgramRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new GenerateProgramRequest("My Program", "Hypertrophy", "PushPullLegs", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var request = new GenerateProgramRequest("", "Hypertrophy", "PushPullLegs", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_FailsValidation()
    {
        var request = new GenerateProgramRequest(new string('a', 201), "Hypertrophy", "PushPullLegs", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void InvalidGoal_FailsValidation()
    {
        var request = new GenerateProgramRequest("Test", "InvalidGoal", "PushPullLegs", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void EmptyGoal_FailsValidation()
    {
        var request = new GenerateProgramRequest("Test", "", "PushPullLegs", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void InvalidTemplate_FailsValidation()
    {
        var request = new GenerateProgramRequest("Test", "Hypertrophy", "InvalidTemplate", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Template);
    }

    [Fact]
    public void EmptyTemplate_FailsValidation()
    {
        var request = new GenerateProgramRequest("Test", "Hypertrophy", "", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Template);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(53)]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidDuration_FailsValidation(int weeks)
    {
        var request = new GenerateProgramRequest("Test", "Hypertrophy", "PushPullLegs", weeks, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DurationWeeks);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(52)]
    [InlineData(12)]
    public void ValidDuration_PassesValidation(int weeks)
    {
        var request = new GenerateProgramRequest("Test", "Hypertrophy", "PushPullLegs", weeks, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.DurationWeeks);
    }

    [Fact]
    public void InvalidStartDate_FailsValidation()
    {
        var request = new GenerateProgramRequest("Test", "Hypertrophy", "PushPullLegs", 12, "not-a-date");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Theory]
    [InlineData("hypertrophy")]
    [InlineData("HYPERTROPHY")]
    [InlineData("Hypertrophy")]
    public void GoalIsCaseInsensitive(string goal)
    {
        var request = new GenerateProgramRequest("Test", goal, "PushPullLegs", 12, "2025-01-01");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Goal);
    }
}

public class UpdateProgramRequestValidatorTests
{
    private readonly UpdateProgramRequestValidator _validator = new();

    [Fact]
    public void EmptyRequest_PassesValidation()
    {
        var request = new UpdateProgramRequest(null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyName_FailsValidation()
    {
        var request = new UpdateProgramRequest("", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void NameTooLong_FailsValidation()
    {
        var request = new UpdateProgramRequest(new string('a', 201), null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ValidName_PassesValidation()
    {
        var request = new UpdateProgramRequest("Updated Name", null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void InvalidStatus_FailsValidation()
    {
        var request = new UpdateProgramRequest(null, null, "InvalidStatus");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Active")]
    [InlineData("Completed")]
    [InlineData("Archived")]
    public void ValidStatus_PassesValidation(string status)
    {
        var request = new UpdateProgramRequest(null, null, status);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void DescriptionTooLong_FailsValidation()
    {
        var request = new UpdateProgramRequest(null, new string('a', 2001), null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
