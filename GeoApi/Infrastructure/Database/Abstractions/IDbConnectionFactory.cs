using System.Data;
using Npgsql;

namespace GeoApi.Infrastructure.Database.Abstractions;

public interface IDbConnectionFactory
{
    public IDbConnection CreateConnection();
    public Task<IDbConnection> CreateConnectionAsync(CancellationToken token = default);
}