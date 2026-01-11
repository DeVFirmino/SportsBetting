using FluentValidation;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Application.UseCases.Wallet.Deposit;

public class DepositValidator : AbstractValidator<RequestDepositJson>
{
    public DepositValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage(ResourcesMessagesException.AMOUNT_INVALID);
    }
    
     
}