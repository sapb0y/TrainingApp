using FluentAssertions;
using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class CoachApplicationValidatorTests
{
    private readonly SubmitCoachApplicationRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new SubmitCoachApplicationRequest(
            "CSCS, 5 years experience", 10, "Scale coaching business", null);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyCredentials_Fails()
    {
        var request = new SubmitCoachApplicationRequest(
            "", 10, "Scale coaching business", null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Credentials);
    }

    [Fact]
    public void NegativeClientCount_Fails()
    {
        var request = new SubmitCoachApplicationRequest(
            "CSCS", -1, "Scale coaching business", null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CurrentClientCount);
    }

    [Fact]
    public void EmptyBusinessGoal_Fails()
    {
        var request = new SubmitCoachApplicationRequest(
            "CSCS", 10, "", null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BusinessGoal);
    }
}
