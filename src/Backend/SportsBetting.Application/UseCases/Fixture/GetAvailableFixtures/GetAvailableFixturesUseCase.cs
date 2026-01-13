using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Services.ExternalApis;

namespace SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;

public class GetAvailableFixturesUseCase : IGetAvailableFixtureUseCase
{
    private readonly IFootballApiService _footballApiService;

    public GetAvailableFixturesUseCase(IFootballApiService footballApiService)
    {
        _footballApiService = footballApiService;
    }

    public async Task<List<ResponseFixtureJson>> Execute()
    {
        var fixtures = await _footballApiService.GetUpcomingFixtures();

        var response = fixtures.Select(f => new ResponseFixtureJson
        {
            FixtureId = f.FixtureId,
            HomeTeam = f.HomeTeam,
            AwayTeam = f.AwayTeam,
            Date = f.Date,
            HomeWinOdds = f.HomeWinOdds,
            DrawOdds = f.DrawOdds,
            AwayWinOdds = f.AwayWinOdds
        }).ToList();
        //tO DO VALIDATORS FOR BETTING

        return response;
    }
}
