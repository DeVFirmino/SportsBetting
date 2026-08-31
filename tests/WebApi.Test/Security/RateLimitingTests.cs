using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;

namespace WebApi.Test.Security;

/// <summary>
/// Login, registration and betting are rate limited. This class owns its own factory because the
/// limiter counts requests per application instance, and deliberately exhausting a window here
/// must not spill into the other test classes.
/// </summary>
public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private const int LoginPermitLimit = 5;

    private readonly HttpClient _httpClient;

    public RateLimitingTests(CustomWebApplicationFactory factory) => _httpClient = factory.CreateClient();

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
}
