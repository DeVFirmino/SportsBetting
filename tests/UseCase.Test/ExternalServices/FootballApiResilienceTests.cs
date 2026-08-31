using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Infrastructure.ExternalServices.Football;
using Polly.Timeout;
using SportsBetting.Infrastructure.Options;

namespace UseCase.Test.ExternalServices;

/// <summary>
/// Drives the real Microsoft.Extensions.Http.Resilience pipeline the application registers for
/// API-Football, so the retry, backoff and safe-method rules are checked as configured rather
/// than as described.
/// </summary>
public class FootballApiResilienceTests
{
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task ShouldRetryGetWhenTheUpstreamStatusIsTransient(HttpStatusCode status)
    {
        // Arrange
        var handler = new CountingHandler(_ => new HttpResponseMessage(status));
        var client = CreateResilientClient(handler);

        // Act
        var response = await client.GetAsync("/fixtures");

        // Assert
        response.StatusCode.Should().Be(status);
        handler.Attempts.Should().Be(FootballApiResilience.MaxRetryAttempts + 1);
    }

    [Fact]
    public async Task ShouldStopRetryingWhenAGetEventuallySucceeds()
    {
        // Arrange
        var handler = new CountingHandler(attempt => attempt < 3
            ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            : new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateResilientClient(handler);

        // Act
        var response = await client.GetAsync("/fixtures");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task ShouldRetryGetWhenTheAttemptTimesOut()
    {
        // Arrange
        var handler = new CountingHandler(attempt => attempt < 3
            ? throw new TimeoutRejectedException("attempt timed out")
            : new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateResilientClient(handler);

        // Act
        var response = await client.GetAsync("/fixtures");

        // Assert
        // The per-attempt timeout sits inside the retry, so a slow upstream arrives as
        // TimeoutRejectedException. Leaving it unhandled made the commonest transient failure the
        // one the pipeline never retried.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task ShouldNotRetryWhenTheRequestIsNotAGet()
    {
        // Arrange
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateResilientClient(handler);

        // Act
        var response = await client.PostAsync("/fixtures", new StringContent(string.Empty));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        handler.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task ShouldNotRetryWhenTheUpstreamStatusIsNotTransient()
    {
        // Arrange
        var handler = new CountingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateResilientClient(handler);

        // Act
        var response = await client.GetAsync("/fixtures");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        handler.Attempts.Should().Be(1);
    }

    private static HttpClient CreateResilientClient(HttpMessageHandler primaryHandler)
    {
        ServiceCollection services = new();

        services.AddHttpClient("api-football", client => client.BaseAddress = new Uri("https://football.example"))
            .ConfigurePrimaryHttpMessageHandler(() => primaryHandler)
            // The policy the application registers, configured with the shortest backoff the
            // options allow so the test does not spend seconds asleep.
            .AddResilienceHandler(
                "api-football",
                builder => FootballApiResilience.Configure(builder, new FootballApiOptions
                {
                    BaseUrl = "https://football.example",
                    ApiKey = "test-api-key",
                    RetryDelayMilliseconds = 1,
                }));

        ServiceProvider provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IHttpClientFactory>().CreateClient("api-football");
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _response;

        public CountingHandler(Func<int, HttpResponseMessage> response)
        {
            _response = response;
        }

        public int Attempts { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Attempts++;
            HttpResponseMessage response = _response(Attempts);
            response.RequestMessage = request;

            return Task.FromResult(response);
        }
    }
}
