using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Infrastructure.Options;
using SportsBetting.Infrastructure.ExternalServices.Football;

namespace UseCase.Test.ExternalServices;

public class FootballApiServiceTests
{
    [Fact]
    public async Task ShouldMapFixturesAndSendApiKeyWhenResponseSucceeds()
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
        var result = await service.GetFixturesAsync(CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().BeEquivalentTo(new
        {
            FixtureId = 321,
            HomeTeam = "Home FC",
            AwayTeam = "Away FC",
        });
        capturedRequest!.RequestUri!.PathAndQuery.Should().Be("/fixtures?season=2024&league=140");
        capturedRequest.Headers.GetValues("x-apisports-key").Should().ContainSingle().Which.Should().Be("test-api-key");
    }

    [Fact]
    public async Task ShouldReturnFirstTenWhenMoreThanTenFixturesExist()
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
        var result = await service.GetFixturesAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(10);
        result.Select(item => item.FixtureId).Should().Equal(Enumerable.Range(1, 10));
    }

    [Fact]
    public async Task ShouldReturnEmptyCollectionWhenResponseIsNull()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => JsonResponse("{\"response\":null}")));

        // Act
        var result = await service.GetFixturesAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldReportBadGatewayWhenTheUpstreamAnswersBadGateway()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)));

        // Act
        Func<Task> act = () => service.GetFixturesAsync(CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<UpstreamServiceException>();
        exception.Which.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task ShouldReportServiceUnavailableWhenTheUpstreamAnswersServiceUnavailable()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        // Act
        Func<Task> act = () => service.GetFixturesAsync(CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<UpstreamServiceException>();
        exception.Which.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task ShouldReportServiceUnavailableWhenTheUpstreamRateLimitIsExceeded()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        // Act
        Func<Task> act = () => service.GetFixturesAsync(CancellationToken.None);

        // Assert
        // A quota this API ran out of is not the caller's fault, so it is reported as an
        // unavailable upstream rather than passed through as the caller's own 429.
        var exception = await act.Should().ThrowAsync<UpstreamServiceException>();
        exception.Which.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task ShouldReportBadGatewayWhenTheUpstreamRejectsTheApiKey()
    {
        // Arrange
        var service = CreateService(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));

        // Act
        Func<Task> act = () => service.GetFixturesAsync(CancellationToken.None);

        // Assert
        // A rejected key is a deployment problem here, not a bad request from the API's caller.
        var exception = await act.Should().ThrowAsync<UpstreamServiceException>();
        exception.Which.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task ShouldServeTheCachedFixturesWhenCalledAgainWithinTheCacheWindow()
    {
        // Arrange
        var handler = new StubHandler(_ => JsonResponse("""
            {"response":[{"fixture":{"id":321,"date":"2026-08-20T19:45:00Z"},"teams":{"home":{"name":"Home FC"},"away":{"name":"Away FC"}}}]}
            """));
        var service = CreateService(handler);

        // Act
        var first = await service.GetFixturesAsync(CancellationToken.None);
        var second = await service.GetFixturesAsync(CancellationToken.None);

        // Assert
        first.Should().ContainSingle();
        second.Should().BeSameAs(first);
        handler.Calls.Should().Be(1);
    }

    private static FootballApiService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://football.example") };
        return new FootballApiService(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new FootballApiOptions
            {
                BaseUrl = "https://football.example",
                ApiKey = "test-api-key",
                CacheSeconds = 30,
                TimeoutSeconds = 10,
            }));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;

            return Task.FromResult(response(request));
        }
    }
}
