using System.Data.Common;
using Dapper;
using GeoApi.Infrastructure.Database.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeoApi.Infrastructure.Database;

public class DatabaseHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            await using DbConnection connection = await connectionFactory.CreateConnectionAsync(timeout.Token);
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: timeout.Token));

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The database is not reachable.", exception);
        }
    }
}
