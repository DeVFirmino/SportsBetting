using SportsBetting.API.Filters;
using SportsBetting.Application;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Infrastructure.DataAcess.Repositories;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Infrastructure.DataAcess;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.None);
builder.Services.AddMvc(options => options.Filters.Add(typeof(ExceptionFilter)));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


 
// // Repositories DI
// builder.Services.AddScoped<IUserWriteOnlyRepository, UserRepository>();
// builder.Services.AddScoped<IUserReadOnlyRepository, UserRepository>();

//UseCases
// Use cases
builder.Services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Expose OpenAPI document and Swagger UI in Development
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SportsBetting API v1");
        options.RoutePrefix = "swagger"; // access at /swagger
    });
}

// app.UseHttpsRedirection();

app.MapControllers();

app.Run();
