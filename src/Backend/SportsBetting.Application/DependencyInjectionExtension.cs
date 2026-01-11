using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Application.UseCases.User.GetBalance;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Application.UseCases.User.Login.DoLogin;
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application;

/// <summary>
/// Provides extension meth ods for configuring dependency injection for the application layer.
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
        
    }

    public static void AddAutoMapper(IServiceCollection services)
    {
        services.AddScoped<IMapper>(sp =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AutoMapping>();
            });
            return new Mapper(config, sp.GetService);
        });
    }
    
  
}