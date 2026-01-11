using FluentValidation;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public class PlaceBetValidator : AbstractValidator<RequestPlaceBetJson>
{
    public PlaceBetValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(ResourcesMessagesException.BET_AMOUNT_GREATER_THAN_ZERO);
         
        RuleFor(x => x.Odds).GreaterThan(1)
            .WithMessage(ResourcesMessagesException.BET_ODDS_INVALID);
    
        RuleFor(x => x.EventName).NotEmpty()
            .WithMessage(ResourcesMessagesException.BET_EVENT_NAME_REQUIRED);
    
        RuleFor(x => x.BetType).NotEmpty()
            .WithMessage(ResourcesMessagesException.BET_TYPE_REQUIRED);
    }
        
}