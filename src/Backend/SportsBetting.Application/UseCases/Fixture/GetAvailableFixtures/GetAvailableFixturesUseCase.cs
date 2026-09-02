using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Domain.Services.Odds;

namespace SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;

public sealed class GetAvailableFixturesUseCase : IGetAvailableFixtureUseCase
{
    private readonly IFootballApiService _footballApiService;
    private readonly IOddsService _oddsService;

    public GetAvailableFixturesUseCase(IFootballApiService footballApiService, IOddsService oddsService)
    {
        _footballApiService = footballApiService;
        _oddsService = oddsService;
    }

    public async Task<List<FixtureResponse>> Execute(CancellationToken cancellationToken)
    {
        List<FixtureData> fixtures = await _footballApiService.GetFixturesAsync(cancellationToken);

        return fixtures
            .Select(fixture =>
            {
                FixtureOdds odds = _oddsService.GetOdds(fixture.FixtureId);

                return new FixtureResponse
                {
                    FixtureId = fixture.FixtureId,
                    HomeTeam = fixture.HomeTeam,
                    AwayTeam = fixture.AwayTeam,
                    Date = fixture.Date,
                    HomeWinOdds = odds.HomeWin,
                    DrawOdds = odds.Draw,
                    AwayWinOdds = odds.AwayWin,
                };
            })
            .ToList();
    }
}
