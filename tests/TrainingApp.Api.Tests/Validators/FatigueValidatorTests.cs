using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class CreateRecoveryLogRequestValidatorTests
{
    private readonly CreateRecoveryLogRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", 4, 7.5m, 2, 4, 4, 4, "Slept well");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_DateIsEmpty()
    {
        var req = new CreateRecoveryLogRequest("", null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_DateIsInvalidFormat()
    {
        var req = new CreateRecoveryLogRequest("15-01-2025", null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_SleepQualityOutOfRange()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", 0, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.SleepQuality);

        var req2 = new CreateRecoveryLogRequest("2025-01-15", 6, null, null, null, null, null, null);
        var result2 = _validator.TestValidate(req2);
        result2.ShouldHaveValidationErrorFor(x => x.SleepQuality);
    }

    [Fact]
    public void Should_HaveError_When_SleepHoursOutOfRange()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", null, -1m, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.SleepHours);

        var req2 = new CreateRecoveryLogRequest("2025-01-15", null, 25m, null, null, null, null, null);
        var result2 = _validator.TestValidate(req2);
        result2.ShouldHaveValidationErrorFor(x => x.SleepHours);
    }

    [Fact]
    public void Should_HaveError_When_StressLevelOutOfRange()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", null, null, 6, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.StressLevel);
    }

    [Fact]
    public void Should_HaveError_When_EnergyLevelOutOfRange()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", null, null, null, 0, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.EnergyLevel);
    }

    [Fact]
    public void Should_HaveError_When_MoodOutOfRange()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", null, null, null, null, null, 6, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Mood);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", null, null, null, null, null, null, new string('a', 1001));
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_NotHaveError_When_AllFieldsNull()
    {
        var req = new CreateRecoveryLogRequest("2025-01-15", null, null, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class RecalculateRequestValidatorTests
{
    private readonly RecalculateRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_FromIsNull()
    {
        var req = new RecalculateRequest(null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_NotHaveError_When_FromIsValidDate()
    {
        var req = new RecalculateRequest("2025-01-15");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_FromIsInvalidDate()
    {
        var req = new RecalculateRequest("not-a-date");
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.From);
    }
}
