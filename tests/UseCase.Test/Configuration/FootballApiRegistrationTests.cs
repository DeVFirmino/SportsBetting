using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Infrastructure;
using SportsBetting.Infrastructure.ExternalServices.Football;

namespace UseCase.Test.Configuration;

/// <summary>
/// Which fixture source the application gets is a configuration decision: API-Football when a key
/// is set, the offline sample catalogue when it is not or when the offline flag asks for it.
/// </summary>
public class FootballApiRegistrationTests
{
    [Fact]
    public void ShouldResolveOfflineServiceWhenApiKeyIsMissing()
    {
        Dictionary<string, string?> settings = Settings();
        settings["Settings:FootballApi:ApiKey"] = string.Empty;

        ServiceProvider provider = BuildProvider(settings);

        IFootballApiService service = provider.GetRequiredService<IFootballApiService>();

        service.Should().BeOfType<OfflineFootballApiService>();
    }

    [Fact]
    public void ShouldResolveOfflineServiceWhenApiKeySectionHasNoKey()
    {
        Dictionary<string, string?> settings = Settings();
        settings.Remove("Settings:FootballApi:ApiKey");

        ServiceProvider provider = BuildProvider(settings);

        IFootballApiService service = provider.GetRequiredService<IFootballApiService>();

        service.Should().BeOfType<OfflineFootballApiService>();
    }

    [Fact]
    public void ShouldResolveApiFootballServiceWhenApiKeyIsSet()
    {
        ServiceProvider provider = BuildProvider(Settings());

        IFootballApiService service = provider.GetRequiredService<IFootballApiService>();

        service.Should().BeOfType<FootballApiService>();
    }

    [Fact]
    public void ShouldResolveOfflineServiceWhenOfflineFixturesAreForced()
    {
        Dictionary<string, string?> settings = Settings();
        settings["Settings:FootballApi:UseOfflineFixtures"] = "true";

        ServiceProvider provider = BuildProvider(settings);

        IFootballApiService service = provider.GetRequiredService<IFootballApiService>();

        service.Should().BeOfType<OfflineFootballApiService>();
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Development });
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> Settings() => new()
    {
        ["InMemoryTest"] = "true",
        ["Settings:Jwt:SigningKey"] = "a-signing-key-with-at-least-32-chars",
        ["Settings:Jwt:Issuer"] = "sportsbetting-api",
        ["Settings:Jwt:Audience"] = "sportsbetting-clients",
        ["Settings:Jwt:ExpirationTimeMinutes"] = "60",
        ["Settings:FootballApi:BaseUrl"] = "https://football.example",
        ["Settings:FootballApi:ApiKey"] = "test-api-key",
    };
}
