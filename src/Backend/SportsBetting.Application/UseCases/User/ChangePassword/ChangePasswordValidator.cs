using FluentValidation;
using SportsBetting.Application.SharedValidators;
using SportsBetting.Communication.Requests;

namespace SportsBetting.Application.UseCases.User.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator
            (new PasswordValidator<ChangePasswordRequest>());
    }
}