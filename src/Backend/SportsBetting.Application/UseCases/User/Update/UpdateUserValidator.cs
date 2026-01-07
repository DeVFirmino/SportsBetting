using FluentValidation;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Application.UseCases.User.Update;

public class UpdateUserValidator : AbstractValidator<RequestUpdateUserJson>
{
    public UpdateUserValidator()
    {
        RuleFor(request => request.Name).NotEmpty().WithMessage(ResourcesMessagesException.NAME_EMPTY);
        RuleFor(request => request.Email).NotEmpty().WithMessage(ResourcesMessagesException.EMAIL_EMPTY);
         
        
    }
}