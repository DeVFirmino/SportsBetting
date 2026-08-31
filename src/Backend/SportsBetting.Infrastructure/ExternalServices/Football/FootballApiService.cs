using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Infrastructure.ExternalServices.DTOs;
using SportsBetting.Infrastructure.Options;

namespace SportsBetting.Infrastructure.ExternalServices.Football;

public sealed class FootballApiService : IFootballApiService
{
    private const string UpcomingFixturesCacheKey = "api-football-upcoming-fixtures";

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

    public async Task<List<FixtureData>> GetUpcomingFixturesAsync(CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(UpcomingFixturesCacheKey, out List<FixtureData>? cachedFixtures)
            && cachedFixtures is not null)
            return cachedFixtures;

        var request = new HttpRequestMessage(
            HttpMethod.Get, "/fixtures?season=2024&league=140");
            request.Headers.Add("x-apisports-key", _options.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            EnsureUpstreamSucceeded(response);
 
             var content = await response.Content.ReadAsStringAsync(cancellationToken);
        
             var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiFootballResponse<ApiFixtureDto>>(
                 content, 
                 new System.Text.Json.JsonSerializerOptions 
                 { 
                     PropertyNameCaseInsensitive = true 
                 });
             
             if(apiResponse?.Response == null)
                 return [];
             
             var fixtures = apiResponse.Response
                 .Take(10)
                 .Select(f => new FixtureData
                 {
                     FixtureId = f.Fixture.Id,
                     HomeTeam = f.Teams.Home.Name,
                     AwayTeam = f.Teams.Away.Name,
                     Date = f.Fixture.Date,
                     HomeWinOdds = 2.10m,
                     DrawOdds = 3.40m,
                     AwayWinOdds = 3.80m,
                 })
                 .ToList();

             _memoryCache.Set(
                 UpcomingFixturesCacheKey,
                 fixtures,
                 TimeSpan.FromSeconds(_options.CacheSeconds));

             return fixtures;
    }

    /// <summary>
    /// A failure at API-Football is never the API caller's fault, so it is reported as a gateway
    /// problem: 503 while the upstream is out of quota or unavailable, 502 for anything else it
    /// answered with.
    /// </summary>
    private static void EnsureUpstreamSucceeded(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (response.StatusCode is HttpStatusCode.TooManyRequests)
            throw new HttpRequestException(
                "API-Football rate limit exceeded.", null, HttpStatusCode.ServiceUnavailable);

        if (response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
            throw new HttpRequestException(
                "API-Football is unavailable.", null, HttpStatusCode.ServiceUnavailable);

        throw new HttpRequestException(
            $"API-Football answered {(int)response.StatusCode}.", null, HttpStatusCode.BadGateway);
    }
}
