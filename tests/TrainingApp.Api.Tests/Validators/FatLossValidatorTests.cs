using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class LogWeightRequestValidatorTests
{
    private readonly LogWeightRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new LogWeightRequest("2025-01-15", 85.0m, "Morning weigh-in");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_DateIsEmpty()
    {
        var req = new LogWeightRequest("", 85.0m, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_DateIsInvalidFormat()
    {
        var req = new LogWeightRequest("15-01-2025", 85.0m, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_WeightTooLow()
    {
        var req = new LogWeightRequest("2025-01-15", 19m, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void Should_HaveError_When_WeightTooHigh()
    {
        var req = new LogWeightRequest("2025-01-15", 501m, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var req = new LogWeightRequest("2025-01-15", 85.0m, new string('a', 501));
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_NotHaveError_When_NotesNull()
    {
        var req = new LogWeightRequest("2025-01-15", 85.0m, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class StartDeficitRequestValidatorTests
{
    private readonly StartDeficitRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new StartDeficitRequest(85.0m, 75.0m, 0.5m, "Moderate", 6, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_RateTooLow()
    {
        var req = new StartDeficitRequest(85.0m, null, 0.05m, "Moderate", null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.WeeklyRateKg);
    }

    [Fact]
    public void Should_HaveError_When_RateTooHigh()
    {
        var req = new StartDeficitRequest(85.0m, null, 2.5m, "Moderate", null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.WeeklyRateKg);
    }

    [Fact]
    public void Should_HaveError_When_InvalidStrategy()
    {
        var req = new StartDeficitRequest(85.0m, null, 0.5m, "InvalidStrategy", null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Strategy);
    }

    [Fact]
    public void Should_NotHaveError_When_CaseInsensitiveStrategy()
    {
        var req = new StartDeficitRequest(85.0m, null, 0.5m, "conservative", null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Strategy);
    }

    [Fact]
    public void Should_HaveError_When_TargetWeightOutOfRange()
    {
        var req = new StartDeficitRequest(85.0m, 10m, 0.5m, "Moderate", null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.TargetWeightKg);
    }

    [Fact]
    public void Should_HaveError_When_DietBreakIntervalOutOfRange()
    {
        var req = new StartDeficitRequest(85.0m, null, 0.5m, "Moderate", 0, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DietBreakIntervalWeeks);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var req = new StartDeficitRequest(85.0m, null, 0.5m, "Moderate", null, new string('a', 1001));
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}

public class LogNeatRequestValidatorTests
{
    private readonly LogNeatRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new LogNeatRequest("2025-01-15", 10000, "Good walk day");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_DateIsEmpty()
    {
        var req = new LogNeatRequest("", 10000, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_DateIsInvalidFormat()
    {
        var req = new LogNeatRequest("not-a-date", 10000, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_StepCountNegative()
    {
        var req = new LogNeatRequest("2025-01-15", -1, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.StepCount);
    }

    [Fact]
    public void Should_HaveError_When_StepCountTooHigh()
    {
        var req = new LogNeatRequest("2025-01-15", 200001, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.StepCount);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var req = new LogNeatRequest("2025-01-15", 10000, new string('a', 501));
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
