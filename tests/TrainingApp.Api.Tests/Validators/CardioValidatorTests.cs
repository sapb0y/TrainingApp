using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class LogCardioRequestValidatorTests
{
    private readonly LogCardioRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, 5.0m, 145, 165, null, "Easy run");
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_DateInvalidFormat()
    {
        var req = new LogCardioRequest("15-06-2025", "Running", "Zone2", 30, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_ModalityInvalid()
    {
        var req = new LogCardioRequest("2025-06-15", "Skateboarding", "Zone2", 30, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Modality);
    }

    [Fact]
    public void Should_HaveError_When_ZoneInvalid()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone9", 30, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Zone);
    }

    [Fact]
    public void Should_HaveError_When_DurationTooLow()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 0, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Fact]
    public void Should_HaveError_When_DurationTooHigh()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 601, null, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Fact]
    public void Should_HaveError_When_DistanceTooLow()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, 0m, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DistanceKm);
    }

    [Fact]
    public void Should_HaveError_When_DistanceTooHigh()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, 501m, null, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.DistanceKm);
    }

    [Fact]
    public void Should_HaveError_When_HeartRateTooLow()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, null, 29, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AverageHeartRate);
    }

    [Fact]
    public void Should_HaveError_When_HeartRateTooHigh()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, null, 251, null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.AverageHeartRate);
    }

    [Fact]
    public void Should_HaveError_When_MaxHeartRateOutOfRange()
    {
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, null, null, 251, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.MaxHeartRate);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var longNotes = new string('x', 501);
        var req = new LogCardioRequest("2025-06-15", "Running", "Zone2", 30, null, null, null, null, longNotes);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
