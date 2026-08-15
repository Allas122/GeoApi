using System.Data.Common;
using Dapper;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Configuration;
using GeoApi.Infrastructure.Database.Abstractions;
using GeoApi.Infrastructure.Database.Handlers;
using GeoApi.Infrastructure.Database.Implementations;
using GeoApi.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Npgsql;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeoApi.Infrastructure.Database;

public static class DependencyInjection
{
    public static IServiceCollection AddDatabaseConnectionFactory(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        serviceCollection.AddSingleton<DbDataSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.ConnectionString);

            dataSourceBuilder.UseNetTopologySuite();

            if (options.Logging)
            {
                dataSourceBuilder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            }

            return dataSourceBuilder.Build();
        });
        
        serviceCollection.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
        serviceCollection.AddScoped<DbSession>();
        serviceCollection.AddScoped<IDbSession>(sp => sp.GetRequiredService<DbSession>());
        serviceCollection.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DbSession>());
        serviceCollection.AddScoped<ILocationRepository, LocationRepository>();
        serviceCollection.AddScoped<IResourceRepository, ResourceRepository>();
        
        SqlMapper.AddTypeHandler(new SqlGeometryTypeHandler<Point>());

        serviceCollection.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return serviceCollection;
    }
}