using System.Data.Common;

namespace GeoApi.Infrastructure.Database.Abstractions;

public interface IDbConnectionFactory
{
    public Task<DbConnection> CreateConnectionAsync(CancellationToken token = default);
}
