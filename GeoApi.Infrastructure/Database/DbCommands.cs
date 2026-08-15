using System.Data;
using Dapper;

namespace GeoApi.Infrastructure.Database;

public static class DbCommands
{
    public static int? NormalizeTimeout(int commandTimeout)
    {
        return commandTimeout > 0 ? commandTimeout : null;
    }

    public static CommandDefinition Create(
        string sql,
        object? parameters,
        IDbTransaction? transaction,
        int? commandTimeout,
        CancellationToken ct)
    {
        return new CommandDefinition(
            sql,
            parameters,
            transaction,
            commandTimeout,
            cancellationToken: ct);
    }
}
