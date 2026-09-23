using FluentAssertions;
using FluentValidation.Results;
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.User;

public class UpdateUserValidatorTests
{
    [Fact]
    public void ShouldBeValidWhenNameAndEmailAreProvided()
    {
        var validator = new UpdateUserValidator();
        var request = new UpdateUserRequest { Name = "Ada", Email = "ada@example.com" };

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldReturnNameEmptyWhenNameIsEmpty()
    {
        var validator = new UpdateUserValidator();
        var request = new UpdateUserRequest { Name = "", Email = "ada@example.com" };

        ValidationResult result = validator.Validate(request);

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.NAME_EMPTY);
    }

    [Fact]
    public void ShouldReturnEmailEmptyWhenEmailIsEmpty()
    {
        var validator = new UpdateUserValidator();
        var request = new UpdateUserRequest { Name = "Ada", Email = "" };

        ValidationResult result = validator.Validate(request);

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.EMAIL_EMPTY);
    }
}
