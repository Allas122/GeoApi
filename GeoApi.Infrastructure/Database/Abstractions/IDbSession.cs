using System.Data.Common;

namespace GeoApi.Infrastructure.Database.Abstractions;

public interface IDbSession
{
    Task<DbConnectionLease> LeaseAsync(CancellationToken ct = default);
}

public readonly struct DbConnectionLease : IAsyncDisposable
{
    private readonly bool _owned;

    public DbConnectionLease(DbConnection connection, DbTransaction? transaction, bool owned)
    {
        Connection = connection;
        Transaction = transaction;
        _owned = owned;
    }

    public DbConnection Connection { get; }

    public DbTransaction? Transaction { get; }

    public ValueTask DisposeAsync()
    {
        return _owned ? Connection.DisposeAsync() : ValueTask.CompletedTask;
    }
}
