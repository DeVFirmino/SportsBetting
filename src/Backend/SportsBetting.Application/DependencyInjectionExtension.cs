using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Application.UseCases.Bet.GetUserBets;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;
using SportsBetting.Application.UseCases.User.ChangePassword;
using SportsBetting.Application.UseCases.User.GetBalance;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Application.UseCases.User.Profile;
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Application.UseCases.Wallet.Deposit;

namespace SportsBetting.Application;

/// <summary>
/// Provides extension methods for configuring dependency injection for the application layer.
/// </summary>
public static class DependencyInjectionExtension
{

    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        AddAutoMapper(services);
        AddUseCases(services);
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IDoLoginUseCase, DoLoginUseCase>();
        services.AddScoped<IGetUserProfileUseCase, GetUserProfileUseCase>();
        services.AddScoped<IUpdateUserUseCase, UpdateUserUseCase>();
        services.AddScoped<IDepositUseCase, DepositUseCase>();
        services.AddScoped<IGetBalanceUseCase, GetBalanceUseCase>();
        services.AddScoped<IPlaceBetUseCase, PlaceBetUseCase>();
        services.AddScoped<IGetAvailableFixtureUseCase, GetAvailableFixturesUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IGetUserBetsUseCase,  GetUserBetsUseCase>();
        services.AddScoped<IGetBetByIdUseCase, GetBetByIdUseCase>(); 
    }

    private static void AddAutoMapper(IServiceCollection services)
    {
        // The configuration compiles every mapping plan, so it is built once for the whole
        // application; only the Mapper itself stays scoped so resolvers can use scoped services.
        services.AddSingleton(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapping>();
            }, loggerFactory);
        });

        services.AddScoped<IMapper>(sp =>
            new Mapper(sp.GetRequiredService<MapperConfiguration>(), sp.GetService));
    }
}
