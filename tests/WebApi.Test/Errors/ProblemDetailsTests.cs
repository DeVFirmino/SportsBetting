using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;

namespace WebApi.Test.Errors;

/// <summary>
/// Every error leaves the API through the one global exception boundary as an ASP.NET Core
/// ProblemDetails body, so clients get one error shape instead of one per endpoint.
/// </summary>
public class ProblemDetailsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public ProblemDetailsTests(CustomWebApplicationFactory factory) => _httpClient = factory.CreateClient();

    [Fact]
    public async Task ShouldReturnProblemDetailsWhenRegistrationIsInvalid()
    {
        var request = new RegisterUserRequest
        {
            Name = string.Empty,
            Email = "not-an-email",
            Password = "1",
        };

        var response = await _httpClient.PostAsJsonAsync("user/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(400);
        root.GetProperty("title").GetString().Should().Be("Validation failed");
        root.GetProperty("instance").GetString().Should().Be("/user/register");
        root.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldReturnProblemDetailsWhenCredentialsAreInvalid()
    {
        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "whatever",
        };

        var response = await _httpClient.PostAsJsonAsync("login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(401);
        root.GetProperty("title").GetString().Should().Be("Unauthorized");
        root.GetProperty("errors").EnumerateArray().Should().ContainSingle();
    }
}
