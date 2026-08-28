using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Services.ExternalApis;

namespace SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;

public sealed class GetAvailableFixturesUseCase : IGetAvailableFixtureUseCase
{
    private readonly IFootballApiService _footballApiService;

    public GetAvailableFixturesUseCase(IFootballApiService footballApiService)
    {
        _footballApiService = footballApiService;
    }

    public async Task<List<FixtureResponse>> Execute(CancellationToken cancellationToken)
    {
        var fixtures = await _footballApiService.GetUpcomingFixturesAsync(cancellationToken);

        var response = fixtures.Select(f => new FixtureResponse
        {
            FixtureId = f.FixtureId,
            HomeTeam = f.HomeTeam,
            AwayTeam = f.AwayTeam,
            Date = f.Date,
            HomeWinOdds = f.HomeWinOdds,
            DrawOdds = f.DrawOdds,
            AwayWinOdds = f.AwayWinOdds
        }).ToList();
        return response;
    }
}
