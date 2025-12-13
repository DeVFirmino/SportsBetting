using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SportsBetting.Application.Services.AutoMapper;
using SportsBetting.Infrastructure.DataAcess;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAcess.Repositories;
using SportsBetting.Application.UseCases.User.Register;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// DbContext registration (InMemory by default; can be changed to SQL provider later)
builder.Services.AddDbContext<SportsBettingDbContext>(opt =>
    opt.UseInMemoryDatabase("SportsBettingDb"));

// AutoMapper registration (manual, since DI extension doesn't support  )
builder.Services.AddSingleton<IMapper>(sp =>
{
    var config = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile(new AutoMapping());
    }, null);
    return new Mapper(config);
});

// Repositories DI
builder.Services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
builder.Services.AddScoped<IUserReadOnlyRepository, UserRepository>();
builder.Services.AddScoped<RegisterUserUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapControllers();

app.Run();
