using FluentAssertions;
using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;
using TrainingApp.Core.Entities;

namespace TrainingApp.Api.Tests.Validators;

public class CreateWorkoutRequestValidatorTests
{
    private readonly CreateWorkoutRequestValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_NameIsEmpty()
    {
        var request = new CreateWorkoutRequest("", DateTimeOffset.UtcNow, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_HaveError_When_NameIsTooLong()
    {
        var request = new CreateWorkoutRequest(new string('a', 201), DateTimeOffset.UtcNow, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_HaveError_When_NotesAreTooLong()
    {
        var request = new CreateWorkoutRequest("Valid Name", DateTimeOffset.UtcNow, new string('a', 2001));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var request = new CreateWorkoutRequest("Push Day", DateTimeOffset.UtcNow.AddDays(1), "Chest and triceps");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateWorkoutRequestValidatorTests
{
    private readonly UpdateWorkoutRequestValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_NameIsEmptyString()
    {
        var request = new UpdateWorkoutRequest("", null, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_HaveError_When_SessionRpeIsOutOfRange_Low()
    {
        var request = new UpdateWorkoutRequest(null, null, null, null, 0);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SessionRpe);
    }

    [Fact]
    public void Should_HaveError_When_SessionRpeIsOutOfRange_High()
    {
        var request = new UpdateWorkoutRequest(null, null, null, null, 11);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SessionRpe);
    }

    [Fact]
    public void Should_NotHaveError_When_AllFieldsAreNull()
    {
        var request = new UpdateWorkoutRequest(null, null, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_NotHaveError_When_ValidSessionRpe()
    {
        var request = new UpdateWorkoutRequest(null, null, null, WorkoutStatus.Completed, 8);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateWorkoutSetRequestValidatorTests
{
    private readonly CreateWorkoutSetRequestValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_ExerciseIdIsEmpty()
    {
        var request = new CreateWorkoutSetRequest(Guid.Empty, 1, 10, 100m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ExerciseId);
    }

    [Fact]
    public void Should_HaveError_When_SetNumberIsZero()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 0, 10, 100m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SetNumber);
    }

    [Fact]
    public void Should_HaveError_When_SetNumberIsNegative()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), -1, 10, 100m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SetNumber);
    }

    [Fact]
    public void Should_HaveError_When_TargetRepsIsZero()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 1, 0, 100m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetReps);
    }

    [Fact]
    public void Should_HaveError_When_TargetRepsIsTooHigh()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 1, 101, 100m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetReps);
    }

    [Fact]
    public void Should_HaveError_When_TargetWeightIsNegative()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 1, 10, -50m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetWeight);
    }

    [Fact]
    public void Should_HaveError_When_TargetWeightIsZero()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 1, 10, 0m, false);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.TargetWeight);
    }

    [Fact]
    public void Should_NotHaveError_When_ValidRequest()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 1, 10, 100m, false);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_NotHaveError_When_NullOptionalFields()
    {
        var request = new CreateWorkoutSetRequest(Guid.NewGuid(), 1, null, null, false);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateWorkoutSetRequestValidatorTests
{
    private readonly UpdateWorkoutSetRequestValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_RpeIsOutOfRange_Low()
    {
        var request = new UpdateWorkoutSetRequest(null, null, null, null, 0m, null, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Rpe);
    }

    [Fact]
    public void Should_HaveError_When_RpeIsOutOfRange_High()
    {
        var request = new UpdateWorkoutSetRequest(null, null, null, null, 11m, null, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Rpe);
    }

    [Fact]
    public void Should_HaveError_When_RirIsNegative()
    {
        var request = new UpdateWorkoutSetRequest(null, null, null, null, null, -1, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Rir);
    }

    [Fact]
    public void Should_HaveError_When_RirIsTooHigh()
    {
        var request = new UpdateWorkoutSetRequest(null, null, null, null, null, 11, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Rir);
    }

    [Fact]
    public void Should_HaveError_When_ActualWeightIsNegative()
    {
        var request = new UpdateWorkoutSetRequest(null, null, null, -100m, null, null, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ActualWeight);
    }

    [Fact]
    public void Should_NotHaveError_When_ValidRpe()
    {
        var request = new UpdateWorkoutSetRequest(null, null, 8, 100m, 8.5m, 2, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_NotHaveError_When_AllNull()
    {
        var request = new UpdateWorkoutSetRequest(null, null, null, null, null, null, null, null, null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
