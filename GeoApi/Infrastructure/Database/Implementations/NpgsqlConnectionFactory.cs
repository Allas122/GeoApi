using System.Data;
using System.Data.Common;
using GeoApi.Infrastructure.Database.Abstractions;
using Npgsql;

namespace GeoApi.Infrastructure.Database.Implementations;

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly DbDataSource _npgsqlDataSource;
    
    public NpgsqlConnectionFactory(DbDataSource npgsqlDataSource)
    {
        _npgsqlDataSource =  npgsqlDataSource;
    }
    public IDbConnection CreateConnection()
    {
        return _npgsqlDataSource.OpenConnection();
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default)
    {
        return await _npgsqlDataSource.OpenConnectionAsync(token);
    }
}