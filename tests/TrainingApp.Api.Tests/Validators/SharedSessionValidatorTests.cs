using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class CreateSharedSessionRequestValidatorTests
{
    private readonly CreateSharedSessionRequestValidator _validator = new();

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var req = new CreateSharedSessionRequest(Guid.NewGuid(), "2026-03-15", null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_HaveError_When_PartnershipIdEmpty()
    {
        var req = new CreateSharedSessionRequest(Guid.Empty, "2026-03-15", null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.PartnershipId);
    }

    [Fact]
    public void Should_HaveError_When_DateInvalid()
    {
        var req = new CreateSharedSessionRequest(Guid.NewGuid(), "not-a-date", null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_NotHaveError_When_ValidDate()
    {
        var req = new CreateSharedSessionRequest(Guid.NewGuid(), "2026-01-01", null, null, null);
        var result = _validator.TestValidate(req);
        result.ShouldNotHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void Should_HaveError_When_NotesTooLong()
    {
        var req = new CreateSharedSessionRequest(Guid.NewGuid(), "2026-03-15", null, null, new string('x', 501));
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}
