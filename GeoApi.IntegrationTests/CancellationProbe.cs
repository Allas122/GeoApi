using System.Data.Common;
using Dapper;

namespace GeoApi.IntegrationTests;

public static class CancellationProbe
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(15);

    public static Task LockLocationsAsync(DbConnection connection, DbTransaction transaction)
    {
        return connection.ExecuteAsync(
            "LOCK TABLE public.locations IN ACCESS EXCLUSIVE MODE",
            transaction: transaction);
    }

    public static async Task<int> CountLocationsAsync(PostgresFixture fixture)
    {
        await using DbConnection connection = await fixture.OpenAsync();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM public.locations");
    }

    public static async Task<int> CountBlockedInsertsAsync(PostgresFixture fixture)
    {
        await using DbConnection connection = await fixture.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*)
            FROM pg_stat_activity
            WHERE datname = current_database()
              AND pid <> pg_backend_pid()
              AND state = 'active'
              AND query LIKE '%INSERT INTO public.locations%'
            """);
    }

    public static async Task WaitForBlockedInsertAsync(PostgresFixture fixture, bool expected)
    {
        DateTime deadline = DateTime.UtcNow + WaitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            int count = await CountBlockedInsertsAsync(fixture);
            if (expected == count > 0)
            {
                return;
            }

            await Task.Delay(50);
        }

        Assert.Fail(expected
            ? "The insert never reached the database."
            : "The insert was still running on the server after cancellation.");
    }
}
