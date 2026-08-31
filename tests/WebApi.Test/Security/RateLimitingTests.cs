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

    private static Task<HttpResponseMessage> Deposit(HttpClient client)
        => client.PostAsJsonAsync("wallet/deposit", new DepositRequest { Amount = 1m });

    private async Task<HttpClient> RegisteredClientAsync()
    {
        HttpClient client = _factory.CreateClient();

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
