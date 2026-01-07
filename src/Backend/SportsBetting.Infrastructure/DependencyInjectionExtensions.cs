using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.DataAcess;
using SportsBetting.Infrastructure.DataAcess.Repositories;
using SportsBetting.Infrastructure.Extensions;
using SportsBetting.Infrastructure.Security.Cryptography;
using SportsBetting.Infrastructure.Security.Tokens.Access;
using SportsBetting.Infrastructure.Security.Tokens.Access.Generator;
using SportsBetting.Infrastructure.Security.Tokens.Access.Validator;
using SportsBetting.Infrastructure.Services.LoggedUser;

namespace SportsBetting.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    { 
        AddPasswordEncrypter(services, configuration);
        AddRepositories(services);
        AddLoggedUser(services);
        AddTokens(services, configuration);
        
        if (configuration.IsUnitTestEnvironment())
            return;
        
        AddDbContext(services, configuration);
    }

    

    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.ConnectionString();

        services.AddDbContext<SportsBettingDbContext>(options =>
            options.UseSqlServer(connectionString));
         
    }

    // public static void AddAutoMapper(IServiceCollection services)
    // {
    //     services.AddScoped(options => new AutoMapper.MapperConfiguration(options =>
    //     {
    //         options.AddProfile(new AutoMapping());
    //     }).CreateMa
    //
    // }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
        services.AddScoped<IUserReadOnlyRepository, UserRepository>();
        services.AddScoped<IUserUpdateOnlyRepository, UserRepository>();
    }
    
    private static void AddTokens(IServiceCollection services, IConfiguration configuration)
    {
        var expirationTimeMinutes = configuration.GetValue<uint>("Settings:Jwt:ExpirationTimeMinutes");
        var signingKey = configuration.GetValue<string>("Settings:Jwt:SigningKey");

        services.AddScoped<IAccessTokenGenerator>(option => new JwtTokenGenerator(expirationTimeMinutes, signingKey!));
        services.AddScoped<IAccessTokenValidator>(option => new JwtTokenValidator(signingKey!));   
    }
    
    private static void AddLoggedUser(IServiceCollection services) => services.AddScoped<ILoggedUser, LoggedUser>();
    
    private static void AddPasswordEncrypter(IServiceCollection services, IConfiguration configuration)
    {
        var additionalKey = configuration.GetValue<string>("Settings:Password:AdditionalKey");
        
        services.AddScoped<IPasswordEncrypter>(options => new Sha512Encrypter(additionalKey!));
    }
}

