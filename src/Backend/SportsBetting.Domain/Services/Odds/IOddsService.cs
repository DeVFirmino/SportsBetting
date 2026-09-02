namespace SportsBetting.Domain.Services.Odds;

/// <summary>
/// Owns the price of every market. The football provider supplies fixtures and teams only;
/// odds never come from the client or from the provider.
/// </summary>
public interface IOddsService
{
    FixtureOdds GetOdds(int fixtureId);
}
