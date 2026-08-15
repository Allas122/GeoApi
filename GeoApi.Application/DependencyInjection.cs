using GeoApi.Application.Abstractions;
using GeoApi.Application.Implementations;

using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IResourceService, ResourceService>();
        return services;
    }
}