using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
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
        ServiceProvider provider = BuildProvider(ValidSettings());

        JwtOptions jwt = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        FootballApiOptions football = provider.GetRequiredService<IOptions<FootballApiOptions>>().Value;

        provider.GetRequiredService<IStartupValidator>().Invoking(validator => validator.Validate())
            .Should().NotThrow();

        jwt.Issuer.Should().Be("sportsbetting-api");
        jwt.Audience.Should().Be("sportsbetting-clients");
        jwt.ExpirationTimeMinutes.Should().Be(60);
        football.BaseUrl.Should().Be("https://football.example");
        football.CacheSeconds.Should().Be(30);
        football.TimeoutSeconds.Should().Be(10);
    }

    [Theory]
    [InlineData("Settings:Jwt:SigningKey", "too-short")]
    [InlineData("Settings:Jwt:Issuer", "")]
    [InlineData("Settings:Jwt:Audience", "")]
    [InlineData("Settings:Jwt:ExpirationTimeMinutes", "0")]
    [InlineData("Settings:FootballApi:BaseUrl", "not-a-url")]
    [InlineData("Settings:FootballApi:CacheSeconds", "0")]
    [InlineData("Settings:FootballApi:TimeoutSeconds", "0")]
    [InlineData("Settings:FootballApi:Season", "1999")]
    [InlineData("Settings:FootballApi:LeagueId", "0")]
    public void ShouldFailOnStartWhenASettingIsInvalid(string key, string value)
    {
        Dictionary<string, string?> settings = ValidSettings();
        settings[key] = value;

        ServiceProvider provider = BuildProvider(settings);

        Action validate = () => provider.GetRequiredService<IStartupValidator>().Validate();

        validate.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ShouldFailOnStartWhenTheConnectionStringIsMissing()
    {
        Dictionary<string, string?> settings = ValidSettings();
        settings.Remove("InMemoryTest");
        settings["ConnectionStrings:DefaultConnection"] = string.Empty;

        ServiceProvider provider = BuildProvider(settings);

        Action validate = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // The DbContext resolves its connection string from these options, so an empty one has to
        // stop the application at startup rather than at the first query.
        validate.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void ShouldStartWithoutApiKeyWhenEnvironmentIsNotProduction()
    {
        Dictionary<string, string?> settings = ValidSettings();
        settings["Settings:FootballApi:ApiKey"] = string.Empty;

        ServiceProvider provider = BuildProvider(settings, Environments.Development);

        Action validate = () => provider.GetRequiredService<IStartupValidator>().Validate();

        validate.Should().NotThrow();
    }

    [Fact]
    public void ShouldFailOnStartWhenApiKeyIsMissingInProduction()
    {
        Dictionary<string, string?> settings = ValidSettings();
        settings["Settings:FootballApi:ApiKey"] = string.Empty;

        ServiceProvider provider = BuildProvider(settings, Environments.Production);

        Action validate = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // A deployment that lost its key must stop, not quietly offer the sample fixtures.
        validate.Should().Throw<OptionsValidationException>()
            .Which.Message.Should().Contain("ApiKey is required in Production");
    }

    [Fact]
    public void ShouldFailOnStartWhenOfflineFixturesAreForcedInProduction()
    {
        Dictionary<string, string?> settings = ValidSettings();
        settings["Settings:FootballApi:UseOfflineFixtures"] = "true";

        ServiceProvider provider = BuildProvider(settings, Environments.Production);

        Action validate = () => provider.GetRequiredService<IStartupValidator>().Validate();

        validate.Should().Throw<OptionsValidationException>();
    }

    private static ServiceProvider BuildProvider(
        Dictionary<string, string?> settings,
        string environmentName = "Development")
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = environmentName });
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        // Keeps most cases off SQL Server; the connection-string case below opts back in.
        ["InMemoryTest"] = "true",
        ["Settings:Jwt:SigningKey"] = "a-signing-key-with-at-least-32-chars",
        ["Settings:Jwt:Issuer"] = "sportsbetting-api",
        ["Settings:Jwt:Audience"] = "sportsbetting-clients",
        ["Settings:Jwt:ExpirationTimeMinutes"] = "60",
        ["Settings:FootballApi:BaseUrl"] = "https://football.example",
        ["Settings:FootballApi:ApiKey"] = "test-api-key",
        ["Settings:FootballApi:CacheSeconds"] = "30",
        ["Settings:FootballApi:TimeoutSeconds"] = "10",
    };
}
