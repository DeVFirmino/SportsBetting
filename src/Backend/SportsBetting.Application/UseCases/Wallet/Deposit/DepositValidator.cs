using FluentValidation;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public sealed class DepositValidator : AbstractValidator<DepositRequest>
{
    public DepositValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .WithMessage(ResourcesMessagesException.AMOUNT_INVALID);
    }
}
