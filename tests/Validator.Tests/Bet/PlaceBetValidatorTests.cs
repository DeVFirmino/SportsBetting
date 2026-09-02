using FluentAssertions;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Enums;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.Bet;

public sealed class PlaceBetValidatorTests
{
    [Theory]
    [InlineData(BettingMarket.HomeWin)]
    [InlineData(BettingMarket.Draw)]
    [InlineData(BettingMarket.AwayWin)]
    public void ShouldBeValidWhenMarketIsSupported(BettingMarket market)
    {
        PlaceBetRequest request = ValidRequest();
        request.Market = market;

        FluentValidation.Results.ValidationResult result = new PlaceBetValidator().Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShouldReturnMarketRequiredWhenMarketIsUnsupported()
    {
        PlaceBetRequest request = ValidRequest();
        request.Market = (BettingMarket)999;

        FluentValidation.Results.ValidationResult result = new PlaceBetValidator().Validate(request);

        result.Errors.Should().Contain(error =>
            error.ErrorMessage == ResourcesMessagesException.BETTING_MARKET_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ShouldReturnStakeErrorWhenStakeIsNotPositive(decimal stake)
    {
        PlaceBetRequest request = ValidRequest();
        request.Stake = stake;

        FluentValidation.Results.ValidationResult result = new PlaceBetValidator().Validate(request);

        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == ResourcesMessagesException.BET_STAKE_GREATER_THAN_ZERO);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ShouldReturnFixtureNotFoundWhenFixtureIdIsInvalid(int fixtureId)
    {
        PlaceBetRequest request = ValidRequest();
        request.FixtureId = fixtureId;

        FluentValidation.Results.ValidationResult result = new PlaceBetValidator().Validate(request);

        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == ResourcesMessagesException.FIXTURE_NOT_FOUND);
    }

    private static PlaceBetRequest ValidRequest() => new()
    {
        FixtureId = 123,
        Stake = 20m,
        Market = BettingMarket.HomeWin,
    };
}
