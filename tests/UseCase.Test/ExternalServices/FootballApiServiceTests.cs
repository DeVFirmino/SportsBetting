using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SportsBetting.Infrastructure.ExternalServices.Football;

namespace UseCase.Test.ExternalServices;

public class FootballApiServiceTests
{
    [Fact]
    public async Task GetUpcomingFixtures_WithSuccessfulResponse_MapsFixturesAndSendsApiKey()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse("""
                {"response":[{"fixture":{"id":321,"date":"2026-08-20T19:45:00Z"},"teams":{"home":{"name":"Home FC"},"away":{"name":"Away FC"}}}]}
                """);
        });
        var service = CreateService(handler);

        // Act
        var result = await service.GetUpcomingFixturesAsync(CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().BeEquivalentTo(new
        {
            FixtureId = 321,
            HomeTeam = "Home FC",
            AwayTeam = "Away FC",
            HomeWinOdds = (decimal?)2.10m,
            DrawOdds = (decimal?)3.40m,
            AwayWinOdds = (decimal?)3.80m
        });
        capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/fixtures?season=2024&league=140");
        capturedRequest.Headers.GetValues("x-apisports-key").Should().ContainSingle("test-api-key");
    }

    [Fact]
    public async Task GetUpcomingFixtures_WithMoreThanTenFixtures_ReturnsFirstTen()
    {
        // Arrange
        var payload = new
        {
            response = Enumerable.Range(1, 12).Select(index => new
            {
                fixture = new { id = index, date = "2026-08-20T19:45:00Z" },
                teams = new
                {
                    home = new { name = $"H{index}" },
                    away = new { name = $"A{index}" }
                }
            })
        };
        var service = CreateService(new StubHandler(_ => JsonResponse(JsonSerializer.Serialize(payload))));

        // Act
        var result = await service.GetUpcomingFixturesAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(10);
        result.Select(item => item.FixtureId).Should().Equal(Enumerable.Range(1, 10));
    }

    [Fact]
    public async Task GetUpcomingFixtures_WithNullResponse_ReturnsEmptyCollection()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => JsonResponse("{\"response\":null}")));

        // Act
        var result = await service.GetUpcomingFixturesAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUpcomingFixtures_WithFailureStatus_ThrowsHttpRequestException()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)));

        // Act
        Func<Task> act = () => service.GetUpcomingFixturesAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static FootballApiService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://football.example") };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Settings:FootballApi:ApiKey"] = "test-api-key"
            })
            .Build();

        return new FootballApiService(client, configuration);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
