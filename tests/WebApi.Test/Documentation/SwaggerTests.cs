using System.Net;
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
}
