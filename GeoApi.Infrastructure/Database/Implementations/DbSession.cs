using System.Data.Common;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Database.Abstractions;

namespace GeoApi.Infrastructure.Database.Implementations;

public sealed class DbSession : IDbSession, IUnitOfWork, IAsyncDisposable
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public DbSession(IDbConnectionFactory factory)
    {
        _dbConnectionFactory = factory;
    }

    public async Task<DbConnectionLease> LeaseAsync(CancellationToken ct = default)
    {
        DbConnection connection = await EnsureConnectionAsync(ct);
        return new DbConnectionLease(connection, _transaction, owned: false);
    }

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active in the current scope.");
        }

        DbConnection connection = await EnsureConnectionAsync(ct);
        _transaction = await connection.BeginTransactionAsync(ct);
        return new TransactionScope(this);
    }

    private async Task<DbConnection> EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection is not null)
        {
            return _connection;
        }

        await _connectionGate.WaitAsync(ct);
        try
        {
            return _connection ??= await _dbConnectionFactory.CreateConnectionAsync(ct);
        }
        finally
        {
            _connectionGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
        _connectionGate.Dispose();
    }

    private async Task ReleaseTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    private async Task ReleaseAsync()
    {
        await ReleaseTransactionAsync();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private sealed class TransactionScope : ITransactionScope
    {
        private readonly DbSession _session;
        private bool _completed;

        public TransactionScope(DbSession session)
        {
            _session = session;
        }

        public async Task CommitAsync(CancellationToken ct = default)
        {
            if (_completed)
            {
                throw new InvalidOperationException("The transaction has already been completed.");
            }

            await _session._transaction!.CommitAsync(ct);
            _completed = true;
        }

        public async Task RollbackAsync(CancellationToken ct = default)
        {
            if (_completed)
            {
                throw new InvalidOperationException("The transaction has already been completed.");
            }

            await _session._transaction!.RollbackAsync(ct);
            _completed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_completed && _session._transaction is not null)
            {
                await _session._transaction.RollbackAsync();
                _completed = true;
            }

            await _session.ReleaseTransactionAsync();
        }
    }
}
