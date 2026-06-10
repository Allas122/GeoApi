using GeoApi.Api.Configuration;

namespace GeoApi.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services)
    {
        services.AddLocalizationConfiguration();
        services.AddOpenApiConfiguration();
        services.AddControllers();
        return services;
    }
}