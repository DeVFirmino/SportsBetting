using FluentAssertions;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Exceptions;
using SportsBetting.Tests.Common.Requests;

namespace SportsBetting.Tests.User.Register;

public class RegisterUserValidatorTest
{
    [Fact]
    public void ShouldBeValidWhenRequestIsValid()
    {
        var validator = new RegisterUserValidator();

        var request = RegisterUserRequestBuilder.Build();
        
        
        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    } 
    
    [Fact]
    public void ShouldReturnErrorWhenNameIsEmpty()
    {
        var validator = new RegisterUserValidator();

        var request = RegisterUserRequestBuilder.Build();
        request.Name = string.Empty;
        
        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().And
            .Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.NAME_EMPTY));
    } 
    
    
    [Fact]
    public void ShouldReturnErrorWhenEmailIsEmpty()
        {
        var validator = new RegisterUserValidator();

        var request = RegisterUserRequestBuilder.Build();
        request.Email = string.Empty;
        
        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().ContainSingle().And
            .Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.EMAIL_EMPTY));
    } 
    
    [Fact]
    public void ShouldReturnErrorWhenEmailIsInvalid()
    {
        var validator = new RegisterUserValidator();

        var request = RegisterUserRequestBuilder.Build();
        request.Email = "email.com";
        
        var result = validator.Validate(request);

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
        
        var request = RegisterUserRequestBuilder.Build(passwordLength);
        
        var result = validator.Validate(request);
        
        result.IsValid.Should().BeFalse();
        
        result.Errors.Should().ContainSingle()
            .And.Contain(e => e.ErrorMessage.Equals(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID));
    }
    
    
}
