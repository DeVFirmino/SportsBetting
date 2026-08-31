using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Tests.Common.Entities;

namespace WebApi.Test;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{

    private SportsBetting.Domain.Entities.User _user = default!;
    private string _password = string.Empty;
    
    
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing")
            .ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault
                    (d => d.ServiceType == typeof(DbContextOptions<SportsBettingDbContext>));
                if (descriptor is not null)
                    services.Remove(descriptor);
                
                //In memory db
                var provider = services.AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();
                
                services.AddDbContext<SportsBettingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                    options.UseInternalServiceProvider(provider);
                });
                
                var serviceProvider = services.BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<SportsBettingDbContext>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                
                dbContext.Database.EnsureDeleted();
                
                StartDatabase(dbContext, passwordHasher);
              
            });
    }

    public string GetEmail() => _user.Email;
    public string GetPassword() => _password;
    
    public string GetName() => _user.Name;

    private void StartDatabase(SportsBettingDbContext dbContext, IPasswordHasher passwordHasher)
    {
        (_user, _password) = UserBuilder.Build();

        _user.Password = passwordHasher.Hash(_user, _password);
        
        dbContext.Users.Add(_user);
                
        dbContext.SaveChanges();
    }
     
}
