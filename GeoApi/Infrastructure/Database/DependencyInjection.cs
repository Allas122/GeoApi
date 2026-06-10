using System.Data.Common;
using Dapper;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Configuration;
using GeoApi.Infrastructure.Database.Abstractions;
using GeoApi.Infrastructure.Database.DataTypes;
using GeoApi.Infrastructure.Database.Handlers;
using GeoApi.Infrastructure.Database.Implementations;
using GeoApi.Infrastructure.Repositories;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Npgsql;

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
            
            dataSourceBuilder.MapComposite<LocationData>("location_data");
            
            return dataSourceBuilder.Build();
        });
        
        serviceCollection.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
        serviceCollection.AddScoped<ILocationRepository, LocationRepository>();
        
        SqlMapper.AddTypeHandler(new SqlGeometryTypeHandler<Point>());
        
        return serviceCollection;
    }
}