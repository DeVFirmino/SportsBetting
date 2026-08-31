using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Communication.Requests;

namespace WebApi.Test.Security;

/// <summary>
/// X-Forwarded-For is only honoured from a trusted proxy. This factory stamps every connection
/// with the loopback address — a member of the ASP.NET default trust lists — so requests here
/// look exactly like traffic arriving through the deployment's own proxy.
/// </summary>
public sealed class TrustedProxyFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter>(new RemoteAddressStartupFilter(IPAddress.Loopback)));
    }
}

/// <summary>
/// A direct caller: the connection's address is a public one that no trust list contains, so the
/// forwarded-headers middleware must ignore whatever X-Forwarded-For the caller sends.
/// </summary>
public sealed class DirectClientFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter>(
                new RemoteAddressStartupFilter(IPAddress.Parse("203.0.113.99"))));
    }
}

/// <summary>
/// TestServer connections carry no remote address at all — and the forwarded-headers middleware
/// only enforces its trust lists against a connection that has one. Stamping an explicit address
/// before the middleware runs is therefore what makes both the trusted and the untrusted path
/// testable; a real TCP connection always has an address, so production never sees the null case.
/// </summary>
file sealed class RemoteAddressStartupFilter : IStartupFilter
{
    private readonly IPAddress _address;

    public RemoteAddressStartupFilter(IPAddress address)
    {
        _address = address;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = _address;
                return nextMiddleware(context);
            });

            next(app);
        };
}

/// <summary>
/// Behind a trusted proxy, the forwarded client address is the rate-limit partition — so distinct
/// clients keep distinct windows, and presenting a token buys nothing on the anonymous endpoints.
/// </summary>
public class ForwardedHeaderRateLimitingTests : IClassFixture<TrustedProxyFactory>
{
    private const int LoginPermitLimit = 5;

    private readonly TrustedProxyFactory _factory;

    public ForwardedHeaderRateLimitingTests(TrustedProxyFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ShouldLeaveAnotherAddressesLoginAllowanceIntactWhenOneAddressExhaustsIt()
    {
        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        HttpClient greedy = ForwardedClient("203.0.113.10");
        HttpClient bystander = ForwardedClient("203.0.113.20");

        for (int attempt = 0; attempt < LoginPermitLimit; attempt++)
        {
            var allowed = await greedy.PostAsJsonAsync("login", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await greedy.PostAsJsonAsync("login", request)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // A different forwarded address keeps its own allowance — the window did not collapse
        // into one shared bucket behind the proxy.
        (await bystander.PostAsJsonAsync("login", request)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldShareTheLoginWindowWhenTwoTokensComeFromTheSameAddress()
    {
        // Registration mints tokens freely, so a token must not buy a fresh login window: the
        // login partition is the address, never the subject the caller chooses to present.
        const string address = "203.0.113.40";

        HttpClient first = await RegisteredClientAsync(address);
        HttpClient second = await RegisteredClientAsync(address);

        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        for (int attempt = 0; attempt < LoginPermitLimit; attempt++)
        {
            var allowed = await first.PostAsJsonAsync("login", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await second.PostAsJsonAsync("login", request)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private HttpClient ForwardedClient(string forwardedFor)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);

        return client;
    }

    private async Task<HttpClient> RegisteredClientAsync(string forwardedFor)
    {
        HttpClient client = ForwardedClient(forwardedFor);

        var registration = new RegisterUserRequest
        {
            Name = "Rate Limit Tester",
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "Password123!",
        };

        var response = await client.PostAsJsonAsync("user/register", registration);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string? accessToken = document.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }
}

/// <summary>
/// A connection that is not a trusted proxy gets its X-Forwarded-For ignored. This is the
/// regression the partitioning depends on: a direct caller must not mint a fresh login window
/// per forged header.
/// </summary>
public class ForgedForwardedHeaderTests : IClassFixture<DirectClientFactory>
{
    private const int LoginPermitLimit = 5;

    private readonly DirectClientFactory _factory;

    public ForgedForwardedHeaderTests(DirectClientFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ShouldKeepOneLoginWindowWhenADirectCallerForgesADifferentAddressPerRequest()
    {
        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        for (int attempt = 0; attempt < LoginPermitLimit; attempt++)
        {
            var forged = ClientForging($"198.51.100.{attempt + 1}");

            var allowed = await forged.PostAsJsonAsync("login", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // The sixth forged address changes nothing: the header never left the caller's own
        // connection partition, so the window is spent.
        var sixth = ClientForging("198.51.100.6");

        (await sixth.PostAsJsonAsync("login", request)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private HttpClient ClientForging(string forwardedFor)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);

        return client;
    }
}
