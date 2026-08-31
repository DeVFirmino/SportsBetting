using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly.CircuitBreaker;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Services.ExternalApis;

namespace WebApi.Test.Errors;

/// <summary>
/// When API-Football is exhausted or broken, the failure must not read as a fault of this API's
/// caller: the global exception boundary answers 502 or 503, not 500.
/// </summary>
public class UpstreamFailureTests : IClassFixture<UpstreamFailureFactory>
{
    private readonly UpstreamFailureFactory _factory;

    public UpstreamFailureTests(UpstreamFailureFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable)]
    public async Task ShouldReportTheUpstreamStatusWhenApiFootballFails(
        HttpStatusCode upstreamStatus,
        HttpStatusCode expected)
    {
        _factory.FootballApi.StatusCode = upstreamStatus;

        HttpClient client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/fixtures");

        response.StatusCode.Should().Be(expected);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        document.RootElement.GetProperty("status").GetInt32().Should().Be((int)expected);
        document.RootElement.GetProperty("title").GetString().Should().Be("Upstream service unavailable");
    }

    [Fact]
    public async Task ShouldReportServiceUnavailableWhenTheCircuitIsOpen()
    {
        _factory.FootballApi.Failure = new BrokenCircuitException();

        try
        {
            HttpClient client = await AuthenticatedClientAsync();

            var response = await client.GetAsync("/fixtures");

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            document.RootElement.GetProperty("title").GetString().Should().Be("Upstream service unavailable");
        }
        finally
        {
            _factory.FootballApi.Failure = null;
        }
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        HttpClient client = _factory.CreateClient();

        var login = new LoginRequest
        {
            Email = _factory.GetEmail(),
            Password = _factory.GetPassword(),
        };

        var loginResponse = await client.PostAsJsonAsync("login", login);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = document.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }
}

public sealed class UpstreamFailureFactory : CustomWebApplicationFactory
{
    public FailingFootballApiService FootballApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFootballApiService>();
            services.AddSingleton<IFootballApiService>(FootballApi);
        });
    }
}

public sealed class FailingFootballApiService : IFootballApiService
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.ServiceUnavailable;

    public Exception? Failure { get; set; }

    public Task<List<FixtureData>> GetUpcomingFixturesAsync(CancellationToken cancellationToken)
    {
        throw Failure ?? new HttpRequestException("API-Football is unavailable.", null, StatusCode);
    }
}
