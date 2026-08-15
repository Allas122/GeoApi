using Dapper;
using GeoApi.Infrastructure.Database.Abstractions;
using GeoApi.Infrastructure.Database.Errors;
using Npgsql;

namespace GeoApi.Infrastructure.Database;

public static class DbLeaseExtensions
{
    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbConnectionLease lease, CommandDefinition command)
    {
        try
        {
            return await lease.Connection.QueryAsync<T>(command);
        }
        catch (PostgresException exception) when (PostgresExceptionTranslator.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(this DbConnectionLease lease, CommandDefinition command)
    {
        try
        {
            return await lease.Connection.QueryFirstOrDefaultAsync<T>(command);
        }
        catch (PostgresException exception) when (PostgresExceptionTranslator.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    public static async Task<T> QuerySingleAsync<T>(this DbConnectionLease lease, CommandDefinition command)
    {
        try
        {
            return await lease.Connection.QuerySingleAsync<T>(command);
        }
        catch (PostgresException exception) when (PostgresExceptionTranslator.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }

    public static async Task<int> ExecuteAsync(this DbConnectionLease lease, CommandDefinition command)
    {
        try
        {
            return await lease.Connection.ExecuteAsync(command);
        }
        catch (PostgresException exception) when (PostgresExceptionTranslator.Translate(exception) is { } translated)
        {
            throw translated;
        }
    }
}
