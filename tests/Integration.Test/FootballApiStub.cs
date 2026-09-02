using SportsBetting.Domain.Services.ExternalApis;

namespace Integration.Test;

/// <summary>
/// Stands in for API-Football. It also marks the point where the real use case leaves the database
/// to make an HTTP call — the window between the idempotency pre-read and the commit — so a test
/// can make something else happen there.
/// </summary>
public sealed class FootballApiStub : IFootballApiService
{
    public const int FixtureId = 12345;

    private readonly Func<Task>? _whileCalling;

    public FootballApiStub(Func<Task>? whileCalling = null)
    {
        _whileCalling = whileCalling;
    }

    public async Task<List<FixtureData>> GetFixturesAsync(CancellationToken cancellationToken)
    {
        if (_whileCalling is not null)
            await _whileCalling();

        return
        [
            new FixtureData
            {
                FixtureId = FixtureId,
                HomeTeam = "Home FC",
                AwayTeam = "Away FC",
                Date = new DateTime(2026, 9, 1, 19, 45, 0, DateTimeKind.Utc),
            },
        ];
    }
}
