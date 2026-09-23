using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.DataAccess.Repositories;
using SportsBetting.Infrastructure.Extensions;
using SportsBetting.Infrastructure.ExternalServices.Football;
using SportsBetting.Infrastructure.Security.Cryptography;
using SportsBetting.Infrastructure.Security.Tokens.Access;
using SportsBetting.Infrastructure.Security.Tokens.Access.Generator;
using SportsBetting.Infrastructure.Services.LoggedUser;
using SportsBetting.Infrastructure.Services.Odds;
using SportsBetting.Domain.Services.Odds;
using SportsBetting.Domain.Services.ExternalApis;
using SportsBetting.Infrastructure.Options;

namespace SportsBetting.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddOptions(services, configuration);
        AddPasswordHashing(services);
        AddRepositories(services);
        AddExternalServices(services);
        AddLoggedUser(services);
        AddOdds(services);
        AddTokens(services);

        if (configuration.IsUnitTestEnvironment())
        {
            return;
        }

        AddDbContext(services);
    }



    private static void AddDbContext(IServiceCollection services)
    {
        services.AddDbContext<SportsBettingDbContext>((provider, options) =>
        {
            DatabaseOptions database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            options.UseSqlServer(database.DefaultConnection);
        });
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
        services.AddScoped<IUserReadOnlyRepository, UserRepository>();
        services.AddScoped<IUserUpdateOnlyRepository, UserRepository>();
        services.AddScoped<IWalletReadOnlyRepository, WalletRepository>();
        services.AddScoped<IWalletWriteOnlyRepository, WalletRepository>();
        services.AddScoped<IWalletUpdateOnlyRepository, WalletRepository>();
        services.AddScoped<IBetReadOnlyRepository, BetRepository>();
        services.AddScoped<IBetWriteOnlyRepository, BetRepository>();
    }

    private static void AddTokens(IServiceCollection services)
    {
        services.AddScoped<IAccessTokenGenerator, JwtTokenGenerator>();
    }

    private static void AddLoggedUser(IServiceCollection services) => services.AddScoped<ILoggedUser, LoggedUser>();

    private static void AddOdds(IServiceCollection services) => services.AddSingleton<IOddsService, FixedOddsService>();

    private static void AddPasswordHashing(IServiceCollection services)
        => services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();

    private static void AddExternalServices(IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<FootballApiService>((provider, client) =>
        {
            FootballApiOptions options = provider.GetRequiredService<IOptions<FootballApiOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddSingleton<OfflineFootballApiService>();

        // Chosen when first resolved, from the bound options, so the configuration the host ends
        // up with (appsettings, environment, user secrets) decides, not the registration order.
        services.AddTransient<IFootballApiService>(provider =>
        {
            FootballApiOptions options = provider.GetRequiredService<IOptions<FootballApiOptions>>().Value;

            return options.ServesOfflineFixtures
                ? provider.GetRequiredService<OfflineFootballApiService>()
                : provider.GetRequiredService<FootballApiService>();
        });
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FootballApiOptions>()
            .Bind(configuration.GetRequiredSection(FootballApiOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<IHostEnvironment>(
                (options, environment) => environment.IsProduction() is false || options.ServesOfflineFixtures is false,
                "Settings:FootballApi:ApiKey is required in Production; the offline fixtures are for local runs.")
            .ValidateOnStart();

        if (configuration.IsUnitTestEnvironment() is false)
        {
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetRequiredSection(DatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
    }
}
