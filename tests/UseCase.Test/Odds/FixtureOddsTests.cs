using FluentAssertions;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services.Odds;

namespace UseCase.Test.Odds;

public sealed class FixtureOddsTests
{
    [Theory]
    [InlineData(BettingMarket.HomeWin, 2.10)]
    [InlineData(BettingMarket.Draw, 3.40)]
    [InlineData(BettingMarket.AwayWin, 3.80)]
    public void ShouldReturnPriceOfMarketWhenMarketIsSupported(BettingMarket market, decimal expected)
    {
        FixtureOdds odds = new(homeWin: 2.10m, draw: 3.40m, awayWin: 3.80m);

        odds.For(market).Should().Be(expected);
    }

    [Fact]
    public void ShouldThrowWhenMarketIsUnknown()
    {
        FixtureOdds odds = new(homeWin: 2.10m, draw: 3.40m, awayWin: 3.80m);

        Action act = () => odds.For((BettingMarket)99);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 3.40, 3.80)]
    [InlineData(2.10, -1, 3.80)]
    [InlineData(2.10, 3.40, 0)]
    public void ShouldThrowWhenAnyOddIsNotPositive(decimal homeWin, decimal draw, decimal awayWin)
    {
        Action act = () => _ = new FixtureOdds(homeWin, draw, awayWin);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
