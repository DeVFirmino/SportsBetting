using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SportsBetting.API.Converters;
using SportsBetting.API.Filters;
using SportsBetting.Application;
using SportsBetting.Communication.Responses;
using SportsBetting.Infrastructure;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options => options.Filters.Add(typeof(ExceptionFilter)))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new StringConverter()));

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        List<string> errors = context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .Where(message => string.IsNullOrWhiteSpace(message) is false)
            .ToList();

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["errors"] = errors;

        return new BadRequestObjectResult(problemDetails);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JSON Web Token (JWT) Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
                }
            },
            new List<string>()
        }
    });
});

builder.Logging.AddFilter("LuckyPennySoftware.AutoMapper.License", LogLevel.None);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// The live demo runs behind a platform proxy, so without this every caller shares the proxy's
// address and the per-address rate-limit partitions collapse into one bucket. The proxy fleet has
// no stable addresses to pin, so the known-proxy lists are cleared — with ForwardLimit at its
// default of 1, only the X-Forwarded-For value appended by the last hop is honoured, which is the
// edge's view of the client. The accepted trade-off: a proxyless direct caller can mint partitions
// by forging the header, which weakens its own limit but cannot exhaust anybody else's.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// The bearer options read the same validated JwtOptions the rest of the application resolves, so
// there is one reader of the setting and ValidateOnStart still governs it.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        JwtOptions options = jwt.Value;

        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });
builder.Services.AddAuthorization();
// Every policy partitions its window. A single process-wide bucket would let one caller exhaust
// the window for everybody, which turns a brute-force guard into a denial of service.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context => PerAddressWindow(context, permitLimit: 5));
    options.AddPolicy("registration", context => PerAddressWindow(context, permitLimit: 5));
    options.AddPolicy("betting", context => PerSubjectWindow(context, permitLimit: 10));
    options.AddPolicy("wallet", context => PerSubjectWindow(context, permitLimit: 10));
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<SportsBettingDbContext>("database", tags: ["ready"]);

builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

WebApplication app = builder.Build();

// Swagger is served in every environment on purpose: the live demo is a
// portfolio piece and the interactive docs are part of what it demonstrates.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "SportsBetting API v1");
    options.RoutePrefix = "swagger";
});

// First in the pipeline: everything downstream that reads the client address — most of all the
// rate-limit partitions — must see the forwarded one.
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

// After authentication on purpose: the limiter partitions authenticated traffic by the subject
// claim, and running it earlier would put every caller in the same anonymous bucket.
app.UseRateLimiter();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
});

// Readiness reaches SQL Server, so it is the probe that says whether the API can serve traffic.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Partitions a fixed window by the authenticated subject, falling back to the remote address, so
/// one caller's burst never spends another caller's allowance. For endpoints behind [Authorize].
/// </summary>
static RateLimitPartition<string> PerSubjectWindow(HttpContext context, int permitLimit)
{
    string partition = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? RemoteAddress(context);

    return FixedWindow(partition, permitLimit);
}

/// <summary>
/// Partitions a fixed window by the remote address only. Login and registration guard against an
/// attacker, and the attacker's identity is the connection, not whatever token they choose to
/// attach: preferring the subject claim on these anonymous endpoints would hand every
/// self-registered account its own fresh brute-force allowance.
/// </summary>
static RateLimitPartition<string> PerAddressWindow(HttpContext context, int permitLimit) =>
    FixedWindow(RemoteAddress(context), permitLimit);

static string RemoteAddress(HttpContext context) =>
    context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

static RateLimitPartition<string> FixedWindow(string partition, int permitLimit)
{
    return RateLimitPartition.GetFixedWindowLimiter(partition, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    });
}

public partial class Program
{
}
