using Microsoft.Extensions.Logging;
using SportsBetting.Domain.Services.ExternalApis;

namespace SportsBetting.Infrastructure.ExternalServices.Football;

/// <summary>
/// Stands in for API-Football when no key is configured, so the deposit, fixtures and bet flow
/// runs locally from the README alone. The catalogue is a fixed sample: the pairings and kickoff
/// times are illustrative, not the real La Liga 2024 schedule, and the ids never change so a
/// request example can reference them.
/// </summary>
public sealed class OfflineFootballApiService : IFootballApiService
{
    public OfflineFootballApiService(ILogger<OfflineFootballApiService> logger)
    {
        logger.LogWarning(
            "No API-Football key is configured: GET /fixtures serves the offline sample fixtures.");
    }

    public Task<List<FixtureData>> GetFixturesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Catalogue());
    }

    private static List<FixtureData> Catalogue() =>
    [
        Fixture(1001, "Real Madrid", "Barcelona", new DateTime(2024, 10, 26, 19, 0, 0, DateTimeKind.Utc)),
        Fixture(1002, "Atletico Madrid", "Sevilla", new DateTime(2024, 10, 27, 15, 15, 0, DateTimeKind.Utc)),
        Fixture(1003, "Real Sociedad", "Athletic Club", new DateTime(2024, 11, 2, 17, 30, 0, DateTimeKind.Utc)),
        Fixture(1004, "Valencia", "Villarreal", new DateTime(2024, 11, 3, 13, 0, 0, DateTimeKind.Utc)),
        Fixture(1005, "Real Betis", "Girona", new DateTime(2024, 11, 9, 20, 0, 0, DateTimeKind.Utc)),
    ];

    private static FixtureData Fixture(int fixtureId, string homeTeam, string awayTeam, DateTime date) => new()
    {
        FixtureId = fixtureId,
        HomeTeam = homeTeam,
        AwayTeam = awayTeam,
        Date = date,
    };
}
