namespace SportsBetting.Domain.Services.ExternalApis;

public interface IFootballApiService
{
    Task<List<FixtureData>> GetUpcomingFixturesAsync(CancellationToken cancellationToken);
}
