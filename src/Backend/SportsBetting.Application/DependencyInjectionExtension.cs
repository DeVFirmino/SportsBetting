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
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Responses;

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

    public static void AddAutoMapper(IServiceCollection services)
    {
        services.AddScoped<IMapper>(sp =>
        {
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapping>();
            }, loggerFactory);
            return new Mapper(config, sp.GetService);
        });
    }
    
  
}
