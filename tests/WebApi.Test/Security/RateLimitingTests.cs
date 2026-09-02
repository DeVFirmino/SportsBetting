using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;

namespace WebApi.Test.Security;

public sealed class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private const int PermitLimit = 5;
    private readonly HttpClient _httpClient;

    public RateLimitingTests(CustomWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsWhenTokenWindowIsExhausted()
    {
        LoginRequest request = new()
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        for (int attempt = 0; attempt < PermitLimit; attempt++)
        {
            HttpResponseMessage allowed = await _httpClient.PostAsJsonAsync("/tokens", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await _httpClient.PostAsJsonAsync("/tokens", request)).StatusCode
            .Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ShouldReturnTooManyRequestsWhenRegistrationWindowIsExhausted()
    {
        RegisterUserRequest request = new()
        {
            Name = string.Empty,
            Email = "invalid",
            Password = "1",
        };

        for (int attempt = 0; attempt < PermitLimit; attempt++)
        {
            HttpResponseMessage allowed = await _httpClient.PostAsJsonAsync("/users", request);
            allowed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        (await _httpClient.PostAsJsonAsync("/users", request)).StatusCode
            .Should().Be(HttpStatusCode.TooManyRequests);
    }
}
