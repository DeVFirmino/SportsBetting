using FluentAssertions;
using FluentValidation.Results;
using SportsBetting.Application.UseCases.User.ChangePassword;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.User;

public class ChangePasswordValidatorTests
{
    [Fact]
    public void ShouldBeValidWhenNewPasswordIsValid()
    {
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest { NewPassword = "new-password" };

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldReturnPasswordEmptyWhenNewPasswordIsEmpty(string password)
    {
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest { NewPassword = password };

        ValidationResult result = validator.Validate(request);

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.PASSWORD_EMPTY);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void ShouldReturnInvalidCredentialsWhenNewPasswordIsTooShort(string password)
    {
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest { NewPassword = password };

        ValidationResult result = validator.Validate(request);

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID);
    }
}
