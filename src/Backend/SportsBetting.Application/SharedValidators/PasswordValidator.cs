using FluentValidation;
using FluentValidation.Validators;
using SportsBetting.Exceptions;

namespace SportsBetting.Application.SharedValidators;

public class PasswordValidator<T> : PropertyValidator<T, string>
{
    public override bool IsValid(ValidationContext<T> context, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            context.MessageFormatter.AppendArgument("ErrorMessage", ResourcesMessagesException.PASSWORD_EMPTY);
            return false;
        }

        if (password.Length < 6)
        {
            context.MessageFormatter.AppendArgument("ErrorMessage",
                ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID);
            return false;
        }

        return true;
    }

    public override string Name => "PasswordValidator";

    protected override string GetDefaultMessageTemplate(string erroCode) => "{ErrorMessage}";
}