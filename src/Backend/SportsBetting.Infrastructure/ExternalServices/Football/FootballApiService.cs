using Microsoft.Extensions.Configuration;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Infrastructure.ExternalServices.DTOs;

namespace SportsBetting.Infrastructure.ExternalServices.Football;

public class FootballApiService : IFootballApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public FootballApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Settings:FootballApi:ApiKey"]!;
    }

    public async Task<List<FixtureData>> GetUpcomingFixtures()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, "/fixtures?season=2024&league=140");
            request.Headers.Add("x-apisports-key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
 
             var content = await response.Content.ReadAsStringAsync();
        
             var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiFootballResponse<ApiFixtureDto>>(
                 content, 
                 new System.Text.Json.JsonSerializerOptions 
                 { 
                     PropertyNameCaseInsensitive = true 
                 });
             
             if(apiResponse?.Response == null) 
                 return new List<FixtureData>();
             
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

             return fixtures;
    }

     
}
 
