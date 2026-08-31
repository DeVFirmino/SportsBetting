using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.BetRepository;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;
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
        AddExternalServices(services, configuration);
        AddLoggedUser(services);
        AddTokens(services);
        
        if (configuration.IsUnitTestEnvironment())
            return;
        
        AddDbContext(services, configuration);
    }

    

    private static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<SportsBettingDbContext>(options =>
            options.UseSqlServer(connectionString));
         
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
        services.AddScoped<IBetUpdateOnlyRepository, BetRepository>();
        services.AddScoped<IWalletTransactionWriteOnlyRepository, WalletTransactionRepository>();
    }
    
    private static void AddTokens(IServiceCollection services)
    {
        services.AddScoped<IAccessTokenGenerator, JwtTokenGenerator>();
    }
    
    private static void AddLoggedUser(IServiceCollection services) => services.AddScoped<ILoggedUser, LoggedUser>();
    
    private static void AddPasswordHashing(IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, IdentityPasswordHasher>();
        services.AddScoped<IPasswordEncrypter>(provider =>
        {
            PasswordOptions options = provider.GetRequiredService<IOptions<PasswordOptions>>().Value;
            return new Sha512Encrypter(options.AdditionalKey);
        });
    }

    private static void AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddHttpClient<IFootballApiService, FootballApiService>(client =>
        {
            FootballApiOptions options = configuration
                .GetRequiredSection(FootballApiOptions.SectionName)
                .Get<FootballApiOptions>()!;
            client.BaseAddress = new Uri(options.BaseUrl);
        })
        .AddResilienceHandler("api-football", FootballApiResilience.Configure);
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
            .ValidateOnStart();

        if (configuration.IsUnitTestEnvironment() is false)
        {
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetRequiredSection(DatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        services.AddOptions<PasswordOptions>()
            .Bind(configuration.GetSection(PasswordOptions.SectionName))
            .ValidateOnStart();
    }
}
