using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace WebApi.Test.Documentation;

/// <summary>
/// The live demo is a portfolio piece and the interactive docs are part of what it demonstrates,
/// so Swagger is served in every environment. This factory runs under "Testing", which is enough
/// to catch a Development-only gate creeping back in.
/// </summary>
public class SwaggerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public SwaggerTests(CustomWebApplicationFactory factory) => _httpClient = factory.CreateClient();

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task ShouldServeSwaggerWhenTheEnvironmentIsNotDevelopment(string path)
    {
        var response = await _httpClient.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldDescribeTheExpectedRestOperations()
    {
        using JsonDocument document = JsonDocument.Parse(
            await _httpClient.GetStringAsync("/swagger/v1/swagger.json"));

        HashSet<string> actual = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject()
                .Select(operation => $"{operation.Name.ToUpperInvariant()} {path.Name}"))
            .ToHashSet();

        actual.Should().BeEquivalentTo(
        [
            "POST /users",
            "GET /users/me",
            "PUT /users/me",
            "PUT /users/me/password",
            "POST /tokens",
            "GET /wallet",
            "POST /wallet/deposits",
            "GET /fixtures",
            "POST /bets",
            "GET /bets",
            "GET /bets/{id}",
        ]);
    }

    [Fact]
    public async Task ShouldDescribeBettingMarketAsTextChoices()
    {
        using JsonDocument document = JsonDocument.Parse(
            await _httpClient.GetStringAsync("/swagger/v1/swagger.json"));

        JsonElement schema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("BettingMarket");

        schema.GetProperty("type").GetString().Should().Be("string");
        schema.GetProperty("enum").EnumerateArray().Select(value => value.GetString())
            .Should().Equal("HomeWin", "Draw", "AwayWin");
    }
}
