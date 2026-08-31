using System.Net;
using FluentAssertions;

namespace WebApi.Test.Health;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public HealthCheckTests(CustomWebApplicationFactory factory) => _httpClient = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task ShouldReportHealthyWhenTheApplicationIsRunning(string path)
    {
        var response = await _httpClient.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
