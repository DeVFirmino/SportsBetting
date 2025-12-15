using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.DataAcess;
using SportsBetting.Infrastructure.DataAcess.Repositories;

namespace SportsBetting.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseType = configuration.GetConnectionString("DefaultConnection");

        AddDbContext(services, configuration);
        AddRepositories(services);
    }

    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

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

    }
}