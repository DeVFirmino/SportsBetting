using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;

namespace WebApi.Test.Security;

/// <summary>
/// The API no longer authenticates through a custom filter: it is ASP.NET Core JSON Web Token
/// (JWT) bearer authentication, so an anonymous or malformed token is rejected by the
/// authentication middleware before any use case runs.
/// </summary>
public class AuthenticationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public AuthenticationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _httpClient = factory.CreateClient();
    }

    [Theory]
    [InlineData("/bet/get-bets")]
    [InlineData("/user")]
    [InlineData("/wallet")]
    public async Task ShouldReturnUnauthorizedWhenNoTokenIsSupplied(string path)
    {
        var response = await _httpClient.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenTheTokenIsNotAValidJsonWebToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");

        var response = await _httpClient.GetAsync("/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldAcceptTheTokenIssuedByLoginWhenCallingAProtectedEndpoint()
    {
        var login = new LoginRequest
        {
            Email = _factory.GetEmail(),
            Password = _factory.GetPassword(),
        };

        var loginResponse = await _httpClient.PostAsJsonAsync("login", login);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var accessToken = document.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync("/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
