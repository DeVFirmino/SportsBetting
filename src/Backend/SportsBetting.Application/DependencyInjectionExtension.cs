using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.Extensions.Logging;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Application.Services.Cryptography;
using SportsBetting.Application.UseCases.User.Register;

namespace SportsBetting.Application;

/// <summary>
/// Provides extension methods for configuring dependency injection for the application layer.
/// </summary>
public static class DependencyInjectionExtension
{

    public static void AddApplication(this IServiceCollection services)
    {
        AddPasswordEncrypter(services);
        AddAutoMapper(services);
        AddUseCases(services);
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
    }

    public static void AddAutoMapper(IServiceCollection services)
    {
        services.AddSingleton<IMapper>(sp =>
        {
            var expression = new MapperConfigurationExpression();
            expression.AddProfile<AutoMapping>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var mapperConfiguration = new MapperConfiguration(expression, loggerFactory);
            return new Mapper(mapperConfiguration, sp.GetService);
        });

    }
    
    private static void AddPasswordEncrypter(IServiceCollection services)
    {
        services.AddScoped(options => new PasswordEncrypter());
    }
}