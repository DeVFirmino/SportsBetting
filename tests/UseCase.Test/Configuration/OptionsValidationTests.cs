using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SportsBetting.Infrastructure;
using SportsBetting.Infrastructure.Options;

namespace UseCase.Test.Configuration;

/// <summary>
/// The JSON Web Token (JWT), API-Football and database settings are bound to typed options and
/// validated with ValidateOnStart, so a misconfigured deployment fails while it is starting
/// instead of on the first request that happens to need the value.
/// </summary>
public class OptionsValidationTests
{
    [Fact]
    public void ShouldBindEveryOptionWhenConfigurationIsComplete()
    {
        // Arrange
        ServiceProvider provider = BuildProvider(ValidSettings());

        // Act
        JwtOptions jwt = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        FootballApiOptions football = provider.GetRequiredService<IOptions<FootballApiOptions>>().Value;

        // Assert
        provider.GetRequiredService<IStartupValidator>().Invoking(validator => validator.Validate())
            .Should().NotThrow();

        jwt.Issuer.Should().Be("sportsbetting-api");
        jwt.Audience.Should().Be("sportsbetting-clients");
        jwt.ExpirationTimeMinutes.Should().Be(60);
        football.BaseUrl.Should().Be("https://football.example");
        football.CacheSeconds.Should().Be(30);
    }

    [Theory]
    [InlineData("Settings:Jwt:SigningKey", "too-short")]
    [InlineData("Settings:Jwt:Issuer", "")]
    [InlineData("Settings:Jwt:Audience", "")]
    [InlineData("Settings:Jwt:ExpirationTimeMinutes", "0")]
    [InlineData("Settings:FootballApi:BaseUrl", "not-a-url")]
    [InlineData("Settings:FootballApi:ApiKey", "")]
    [InlineData("Settings:FootballApi:CacheSeconds", "0")]
    public void ShouldFailOnStartWhenASettingIsInvalid(string key, string value)
    {
        // Arrange
        Dictionary<string, string?> settings = ValidSettings();
        settings[key] = value;

        ServiceProvider provider = BuildProvider(settings);

        // Act
        Action validate = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        validate.Should().Throw<OptionsValidationException>();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        // Keeps the registration off SQL Server; the database options are exercised by the
        // integration tests that actually reach a server.
        ["InMemoryTest"] = "true",
        ["Settings:Jwt:SigningKey"] = "a-signing-key-with-at-least-32-chars",
        ["Settings:Jwt:Issuer"] = "sportsbetting-api",
        ["Settings:Jwt:Audience"] = "sportsbetting-clients",
        ["Settings:Jwt:ExpirationTimeMinutes"] = "60",
        ["Settings:FootballApi:BaseUrl"] = "https://football.example",
        ["Settings:FootballApi:ApiKey"] = "test-api-key",
        ["Settings:FootballApi:CacheSeconds"] = "30",
        ["Settings:Password:AdditionalKey"] = "additional-key",
    };
}
