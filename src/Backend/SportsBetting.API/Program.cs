using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SportsBetting.API.Filters;
using SportsBetting.API.Converters;
using SportsBetting.API.Token;
using SportsBetting.Application;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Infrastructure;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.Extensions;
 
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new StringConverter()));
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.None);
builder.Services.AddMvc(options => options.Filters.Add(typeof(ExceptionFilter)));
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITokenProvider, HttpContextTokenValue>();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

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

app.UseHttpsRedirection();
app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();

await MigrateDatabase();

async Task MigrateDatabase()
{
    if (app.Environment.IsEnvironment("Testing") || builder.Configuration.IsUnitTestEnvironment())
        return;

    var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

    var dbContext = serviceScope.ServiceProvider.GetRequiredService<SportsBettingDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();


public partial class Program
{
 }