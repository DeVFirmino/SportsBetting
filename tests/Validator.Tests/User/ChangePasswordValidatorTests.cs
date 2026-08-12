using FluentAssertions;
using SportsBetting.Application.UseCases.User.ChangePassword;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.User.ChangePassword;

public class ChangePasswordValidatorTests
{
    [Fact]
    public void Validate_WithValidNewPassword_IsValid()
    {
        // Arrange
        var validator = new ChangePasswordValidator();
        var request = new RequestChangePasswordJson { NewPassword = "new-password" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyNewPassword_ReturnsPasswordEmpty(string password)
    {
        // Arrange
        var validator = new ChangePasswordValidator();
        var request = new RequestChangePasswordJson { NewPassword = password };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.PASSWORD_EMPTY);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_WithShortNewPassword_ReturnsInvalidCredentials(string password)
    {
        // Arrange
        var validator = new ChangePasswordValidator();
        var request = new RequestChangePasswordJson { NewPassword = password };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID);
    }
}
