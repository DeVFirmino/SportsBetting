namespace SportsBetting.Domain.Services.ExternalApis;

public interface IFootballApiService
{
    Task<List<FixtureData>> GetFixturesAsync(CancellationToken cancellationToken);
}
