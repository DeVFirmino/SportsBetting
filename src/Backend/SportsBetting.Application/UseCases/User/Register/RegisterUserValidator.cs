using FluentValidation;
using SportsBetting.Application.SharedValidators;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;
 

namespace SportsBetting.Application.UseCases.User.Register;


//Inherintance with the AbstractValidator and passes as a List the requestuserjson.
//Then make a constructor to pass the rules
public class RegisterUserValidator : AbstractValidator<RequestRegisterUserJson>
{
    public RegisterUserValidator() 
    {
        RuleFor(user => user.Name).NotEmpty().WithMessage(ResourcesMessagesException.NAME_EMPTY);
        RuleFor(user => user.Email)
            .NotEmpty().WithMessage(ResourcesMessagesException.EMAIL_EMPTY);
        RuleFor(user => user.Password).SetValidator(new PasswordValidator<RequestRegisterUserJson>());
         
    }
}
