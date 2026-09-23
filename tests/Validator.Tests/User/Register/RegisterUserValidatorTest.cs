using FluentAssertions;
using FluentValidation.Results;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;
using SportsBetting.Tests.Common.Requests;

namespace SportsBetting.Tests.User.Register;

public class RegisterUserValidatorTest
{
    [Fact]
    public void ShouldBeValidWhenRequestIsValid()
    {
        var validator = new RegisterUserValidator();

        RegisterUserRequest request = RegisterUserRequestBuilder.Build();


        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldReturnErrorWhenNameIsEmpty()
    {
        var validator = new RegisterUserValidator();

        RegisterUserRequest request = RegisterUserRequestBuilder.Build();
        request.Name = string.Empty;

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().And
            .Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.NAME_EMPTY));
    }


    [Fact]
    public void ShouldReturnErrorWhenEmailIsEmpty()
    {
        var validator = new RegisterUserValidator();

        RegisterUserRequest request = RegisterUserRequestBuilder.Build();
        request.Email = string.Empty;

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();

        result.Errors.Should().ContainSingle().And
            .Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.EMAIL_EMPTY));
    }

    [Fact]
    public void ShouldReturnErrorWhenEmailIsInvalid()
    {
        var validator = new RegisterUserValidator();

        RegisterUserRequest request = RegisterUserRequestBuilder.Build();
        request.Email = "email.com";

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();

        result.Errors.Should().ContainSingle().And
            .Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.EMAIL_INVALID));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ShouldReturnErrorWhenPasswordIsInvalid(int passwordLength)
    {
        var validator = new RegisterUserValidator();

        RegisterUserRequest request = RegisterUserRequestBuilder.Build(passwordLength);

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();

        result.Errors.Should().ContainSingle()
            .And.Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID));
    }


}
