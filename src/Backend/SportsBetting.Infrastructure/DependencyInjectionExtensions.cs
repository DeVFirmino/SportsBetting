using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAcess;
using SportsBetting.Infrastructure.DataAcess.Repositories;

namespace SportsBetting.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        AddDbContext(services);

        AddRepositories(services);
    }

    private static void AddDbContext(IServiceCollection services)
    {
        var connectionString = "Server=localhost,1433;" +
                               "Database=SportsBetting;" +
                               "User Id=sa;" +
                               "Password=YourStrongPassword123;" +
                               "TrustServerCertificate=True;";
        
        services.AddDbContext<SportsBettingDbContext>(dbContextOptions =>
        {
            dbContextOptions.UseSqlServer(connectionString);
        });
         
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
        services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
        services.AddScoped<IUserReadOnlyRepository, UserRepository>();

    }
}