using FluentAssertions;
using SportsBetting.Application.UseCases.User.ChangePassword;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.User.ChangePassword;

public class ChangePasswordValidatorTests
{
    [Fact]
    public void ShouldBeValidWhenNewPasswordIsValid()
    {
        // Arrange
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest { NewPassword = "new-password" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldReturnPasswordEmptyWhenNewPasswordIsEmpty(string password)
    {
        // Arrange
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest { NewPassword = password };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.PASSWORD_EMPTY);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void ShouldReturnInvalidCredentialsWhenNewPasswordIsTooShort(string password)
    {
        // Arrange
        var validator = new ChangePasswordValidator();
        var request = new ChangePasswordRequest { NewPassword = password };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID);
    }
}
