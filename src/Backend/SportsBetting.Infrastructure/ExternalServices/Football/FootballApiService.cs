using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.ExternalServices.DTOs;
using SportsBetting.Infrastructure.Options;

namespace SportsBetting.Infrastructure.ExternalServices.Football;

public sealed class FootballApiService : IFootballApiService
{
    private const string FixturesCacheKey = "api-football-fixtures";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly FootballApiOptions _options;

    public FootballApiService(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        IOptions<FootballApiOptions> options)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public async Task<List<FixtureData>> GetFixturesAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(FixturesCacheKey, out List<FixtureData>? cachedFixtures)
            && cachedFixtures is not null)
            return cachedFixtures;

        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"/fixtures?season={_options.Season}&league={_options.LeagueId}");
        request.Headers.Add("x-apisports-key", _options.ApiKey);

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new UpstreamServiceException(503, exception.Message);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            throw new UpstreamServiceException(503, "API-Football request timed out.");
        }

        using (response)
        {
            EnsureUpstreamSucceeded(response.StatusCode);

            ApiFootballResponse<ApiFixtureDto>? apiResponse = await response.Content
                .ReadFromJsonAsync<ApiFootballResponse<ApiFixtureDto>>(cancellationToken);

            List<FixtureData> fixtures = apiResponse?.Response?
                .Take(10)
                .Select(fixture => new FixtureData
                {
                    FixtureId = fixture.Fixture.Id,
                    HomeTeam = fixture.Teams.Home.Name,
                    AwayTeam = fixture.Teams.Away.Name,
                    Date = fixture.Fixture.Date,
                    HomeWinOdds = 2.10m,
                    DrawOdds = 3.40m,
                    AwayWinOdds = 3.80m,
                })
                .ToList() ?? [];

            _memoryCache.Set(
                FixturesCacheKey,
                fixtures,
                TimeSpan.FromSeconds(_options.CacheSeconds));

            return fixtures;
        }
    }

    private static void EnsureUpstreamSucceeded(HttpStatusCode statusCode)
    {
        if ((int)statusCode is >= 200 and < 300)
            return;

        if (statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout)
            throw new UpstreamServiceException(503, "API-Football is unavailable.");

        throw new UpstreamServiceException(502, $"API-Football answered {(int)statusCode}.");
    }
}
