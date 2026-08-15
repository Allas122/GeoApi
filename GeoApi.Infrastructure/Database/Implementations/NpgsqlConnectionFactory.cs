using System.Data.Common;
using GeoApi.Infrastructure.Database.Abstractions;

namespace GeoApi.Infrastructure.Database.Implementations;

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly DbDataSource _npgsqlDataSource;

    public NpgsqlConnectionFactory(DbDataSource npgsqlDataSource)
    {
        _npgsqlDataSource =  npgsqlDataSource;
    }
    public async Task<DbConnection> CreateConnectionAsync(CancellationToken token = default)
    {
        return await _npgsqlDataSource.OpenConnectionAsync(token);
    }
}
