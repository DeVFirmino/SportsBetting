using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;

namespace WebApi.Test.Security;

/// <summary>
/// Every rate-limit policy partitions its window. A single process-wide bucket would let one
/// caller lock everybody else out, so the point of these tests is that exhausting one caller's
/// allowance leaves another caller's intact.
/// </summary>
public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private const int LoginPermitLimit = 5;
    private const int WalletPermitLimit = 10;

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public RateLimitingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsWhenTheLoginWindowIsExhausted()
    {
        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        for (int attempt = 0; attempt < LoginPermitLimit; attempt++)
        {
            var allowed = await _httpClient.PostAsJsonAsync("login", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var rejected = await _httpClient.PostAsJsonAsync("login", request);

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ShouldLeaveAnotherUsersAllowanceIntactWhenOneUserExhaustsTheWalletWindow()
    {
        HttpClient greedy = await RegisteredClientAsync();
        HttpClient bystander = await RegisteredClientAsync();

        for (int attempt = 0; attempt < WalletPermitLimit; attempt++)
        {
            var allowed = await Deposit(greedy);
            allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        (await Deposit(greedy)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // The window is keyed on the authenticated subject, so the second user still has all of it.
        (await Deposit(bystander)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ShouldLeaveAnotherAddressesLoginAllowanceIntactWhenOneAddressExhaustsIt()
    {
        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        // Forwarded headers are trusted (see Program.cs), so X-Forwarded-For is the client
        // address the partition sees — which is exactly what a deployment behind a proxy gets.
        HttpClient greedy = _factory.CreateClient();
        greedy.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");
        HttpClient bystander = _factory.CreateClient();
        bystander.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.20");

        for (int attempt = 0; attempt < LoginPermitLimit; attempt++)
        {
            var allowed = await greedy.PostAsJsonAsync("login", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await greedy.PostAsJsonAsync("login", request)).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // A different forwarded address keeps its own allowance — the window did not collapse
        // into one shared bucket.
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

    private static Task<HttpResponseMessage> Deposit(HttpClient client)
        => client.PostAsJsonAsync("wallet/deposit", new DepositRequest { Amount = 1m });

    private async Task<HttpClient> RegisteredClientAsync(string? forwardedFor = null)
    {
        HttpClient client = _factory.CreateClient();

        if (forwardedFor is not null)
            client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);

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
