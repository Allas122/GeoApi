using GeoApi.Infrastructure.Database;

namespace GeoApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddDatabaseConnectionFactory(configuration);
        return services;
    }
}