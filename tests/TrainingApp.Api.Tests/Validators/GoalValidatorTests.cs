using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class CreateGoalRequestValidatorTests
{
    private readonly CreateGoalRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new CreateGoalRequest("Bench 100kg", "Strength", 100m, "kg", null, 80m, "2025-12-31", null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_TitleEmpty()
    {
        var req = new CreateGoalRequest("", "Strength", 100m, "kg", null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_HaveError_When_TitleTooLong()
    {
        var req = new CreateGoalRequest(new string('x', 201), "Strength", 100m, "kg", null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Should_HaveError_When_TypeInvalid()
    {
        var req = new CreateGoalRequest("Test Goal", "InvalidType", 100m, "kg", null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Should_HaveError_When_TargetValueNegative()
    {
        var req = new CreateGoalRequest("Test Goal", "Strength", -10m, "kg", null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TargetValue);
    }

    [Fact]
    public void Should_HaveError_When_TargetDateInvalid()
    {
        var req = new CreateGoalRequest("Test Goal", "Strength", 100m, "kg", null, null, "not-a-date", null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TargetDate);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var req = new CreateGoalRequest("Test Goal", "Strength", 100m, "kg", null, null, null, new string('x', 501));
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_NotHaveError_When_MinimalRequest()
    {
        var req = new CreateGoalRequest("My Goal", "Custom", null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateGoalRequestValidatorTests
{
    private readonly UpdateGoalRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new UpdateGoalRequest("Achieved", 120m, "2025-12-31", "Updated notes");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_StatusInvalid()
    {
        var req = new UpdateGoalRequest("InvalidStatus", null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Should_NotHaveError_When_EmptyRequest()
    {
        var req = new UpdateGoalRequest(null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class AddCheckpointRequestValidatorTests
{
    private readonly AddCheckpointRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new AddCheckpointRequest("2025-06-15", 95m, "Getting closer");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_DateMissing()
    {
        var req = new AddCheckpointRequest("", 95m, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_DateInvalid()
    {
        var req = new AddCheckpointRequest("not-a-date", 95m, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }
}
