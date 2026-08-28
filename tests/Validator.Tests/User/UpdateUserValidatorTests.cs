using FluentAssertions;
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.User.Update;

public class UpdateUserValidatorTests
{
    [Fact]
    public void ShouldBeValidWhenNameAndEmailAreProvided()
    {
        // Arrange
        var validator = new UpdateUserValidator();
        var request = new UpdateUserRequest { Name = "Ada", Email = "ada@example.com" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldReturnNameEmptyWhenNameIsEmpty()
    {
        // Arrange
        var validator = new UpdateUserValidator();
        var request = new UpdateUserRequest { Name = "", Email = "ada@example.com" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.NAME_EMPTY);
    }

    [Fact]
    public void ShouldReturnEmailEmptyWhenEmailIsEmpty()
    {
        // Arrange
        var validator = new UpdateUserValidator();
        var request = new UpdateUserRequest { Name = "Ada", Email = "" };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.EMAIL_EMPTY);
    }
}
