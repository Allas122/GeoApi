using Serilog;

namespace GeoApi.Api.Configuration;

public static class LoggingConfiguration
{
    public static IServiceCollection AddLoggingConfiguration(this IServiceCollection services)
    {
        services.AddSerilog((provider, configuration) => configuration
            .ReadFrom.Configuration(provider.GetRequiredService<IConfiguration>())
            .ReadFrom.Services(provider));

        return services;
    }
}