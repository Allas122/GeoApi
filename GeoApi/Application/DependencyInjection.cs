using GeoApi.Application.Abstractions;
using GeoApi.Application.Implementations;

namespace GeoApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILocationService, LocationService>();
        return services;
    }
}