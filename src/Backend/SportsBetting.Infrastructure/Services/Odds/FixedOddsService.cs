using SportsBetting.Domain.Services.Odds;

namespace SportsBetting.Infrastructure.Services.Odds;

/// <summary>
/// Study pricing: every fixture is offered at the same fixed odds. A real bookmaker would price
/// each fixture separately; this project only needs the odds to be server-owned and stable.
/// </summary>
public sealed class FixedOddsService : IOddsService
{
    private static readonly FixtureOdds StudyOdds = new(homeWin: 2.10m, draw: 3.40m, awayWin: 3.80m);

    public FixtureOdds GetOdds(int fixtureId) => StudyOdds;
}
