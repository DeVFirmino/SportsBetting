using Microsoft.Extensions.Configuration;

namespace SportsBetting.Infrastructure.Extensions;

public static class ConfigurationExtension
{
    public static bool IsUnitTestEnvironment
        (this IConfiguration configuration) => configuration.GetValue<bool>("InMemoryTest") || configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Testing";

    public static string ConnectionString(this IConfiguration configuration)
    {
        return configuration.GetConnectionString("DefaultConnection")!;
    }
}