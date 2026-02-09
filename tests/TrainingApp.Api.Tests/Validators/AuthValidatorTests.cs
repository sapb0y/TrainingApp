using FluentValidation.TestHelper;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Validators;

namespace TrainingApp.Api.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "Password1", "John"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_EmailEmpty()
    {
        var result = _validator.TestValidate(new RegisterRequest("", "Password1", "John"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_EmailInvalid()
    {
        var result = _validator.TestValidate(new RegisterRequest("notanemail", "Password1", "John"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_PasswordTooShort()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "Pass1", "John"));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_When_PasswordNoUppercase()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "password1", "John"));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_When_PasswordNoLowercase()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "PASSWORD1", "John"));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_When_PasswordNoDigit()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "Passwordx", "John"));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Fail_When_DisplayNameEmpty()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "Password1", ""));
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Should_Fail_When_DisplayNameTooShort()
    {
        var result = _validator.TestValidate(new RegisterRequest("user@test.com", "Password1", "J"));
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var result = _validator.TestValidate(new LoginRequest("user@test.com", "password"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_EmailEmpty()
    {
        var result = _validator.TestValidate(new LoginRequest("", "password"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_PasswordEmpty()
    {
        var result = _validator.TestValidate(new LoginRequest("user@test.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public class RefreshRequestValidatorTests
{
    private readonly RefreshRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var result = _validator.TestValidate(new RefreshRequest("sometoken"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Empty()
    {
        var result = _validator.TestValidate(new RefreshRequest(""));
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
