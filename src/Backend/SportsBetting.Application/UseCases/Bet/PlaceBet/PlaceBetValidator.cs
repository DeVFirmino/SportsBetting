using FluentValidation;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Application.UseCases.Bet.PlaceBet;

public sealed class PlaceBetValidator : AbstractValidator<PlaceBetRequest>
{
    public PlaceBetValidator()
    {
        RuleFor(request => request.Stake)
            .GreaterThan(0)
            .WithMessage(ResourcesMessagesException.BET_STAKE_GREATER_THAN_ZERO);

        RuleFor(request => request.Market)
            .NotNull()
            .WithMessage(ResourcesMessagesException.BETTING_MARKET_REQUIRED)
            .IsInEnum()
            .WithMessage(ResourcesMessagesException.BETTING_MARKET_REQUIRED);

        RuleFor(request => request.FixtureId)
            .GreaterThan(0)
            .WithMessage(ResourcesMessagesException.FIXTURE_NOT_FOUND);
    }
}
