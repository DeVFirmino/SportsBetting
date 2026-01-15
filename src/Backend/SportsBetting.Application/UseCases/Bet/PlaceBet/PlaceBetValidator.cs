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

        RuleFor(bet => bet.BetType)
            .NotEmpty()
            .WithMessage(ResourcesMessagesException.BET_TYPE_REQUIRED)
            .Must(x => x == "HomeWin" || x == "Draw" || x == "AwayWin")
            .WithMessage(ResourcesMessagesException.BET_TYPE_REQUIRED);
        
        RuleFor(x => x.FixtureId).GreaterThan(0)
            .WithMessage(ResourcesMessagesException.FIXTURE_NOT_FOUND);
    }
        
}