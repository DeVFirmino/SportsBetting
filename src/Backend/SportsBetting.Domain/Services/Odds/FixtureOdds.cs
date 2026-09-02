using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Services.Odds;

public sealed class FixtureOdds
{
    public FixtureOdds(decimal homeWin, decimal draw, decimal awayWin)
    {
        if (homeWin <= 0 || draw <= 0 || awayWin <= 0)
            throw new ArgumentOutOfRangeException(nameof(homeWin), "Odds must be greater than zero.");

        HomeWin = homeWin;
        Draw = draw;
        AwayWin = awayWin;
    }

    public decimal HomeWin { get; }
    public decimal Draw { get; }
    public decimal AwayWin { get; }

    public decimal For(BettingMarket market)
    {
        return market switch
        {
            BettingMarket.HomeWin => HomeWin,
            BettingMarket.Draw => Draw,
            BettingMarket.AwayWin => AwayWin,
            _ => throw new ArgumentOutOfRangeException(nameof(market)),
        };
    }
}
