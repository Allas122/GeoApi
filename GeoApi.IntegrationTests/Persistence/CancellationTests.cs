using System.Data.Common;
using Dapper;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Database.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class CancellationTests(PostgresFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task CreateOrGet_WithAlreadyCancelledToken_ThrowsAndWritesNothing()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Locations.CreateOrGetAsync(new Coordinate(41.0, 41.0), cts.Token));

        Assert.Equal(0, await CancellationProbe.CountLocationsAsync(Fixture));
    }

    [Fact]
    public async Task GetById_WithAlreadyCancelledToken_Throws()
    {
        int id = await Locations.CreateOrGetAsync(new Coordinate(40.0, 40.0));

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Locations.GetByIdAsync(id, cts.Token));
    }

    [Fact]
    public async Task CreateOrGet_CancelledWhileWaitingForLock_AbortsServerSideStatement()
    {
        await using DbConnection blocker = await Fixture.OpenAsync();
        await using DbTransaction blockingTransaction = await blocker.BeginTransactionAsync();
        await CancellationProbe.LockLocationsAsync(blocker, blockingTransaction);

        using var cts = new CancellationTokenSource();
        Task<int> pending = Locations.CreateOrGetAsync(new Coordinate(42.0, 42.0), cts.Token);

        await CancellationProbe.WaitForBlockedInsertAsync(Fixture, expected: true);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await CancellationProbe.WaitForBlockedInsertAsync(Fixture, expected: false);

        await blockingTransaction.RollbackAsync();
        Assert.Equal(0, await CancellationProbe.CountLocationsAsync(Fixture));
    }

    [Fact]
    public async Task StatementTimeout_BecomesOperationTimedOutException()
    {
        await using AsyncServiceScope scope = NewScope();
        var session = scope.ServiceProvider.GetRequiredService<IDbSession>();
        var locations = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        await using (DbConnectionLease lease = await session.LeaseAsync())
        {
            await lease.Connection.ExecuteAsync("SET statement_timeout = '250ms'");
        }

        await using DbConnection blocker = await Fixture.OpenAsync();
        await using DbTransaction blockingTransaction = await blocker.BeginTransactionAsync();
        await CancellationProbe.LockLocationsAsync(blocker, blockingTransaction);

        var exception = await Assert.ThrowsAsync<OperationTimedOutException>(
            () => locations.CreateOrGetAsync(new Coordinate(43.0, 43.0)));

        Assert.DoesNotContain("statement_timeout", exception.Message, StringComparison.OrdinalIgnoreCase);

        await blockingTransaction.RollbackAsync();
        Assert.Equal(0, await CancellationProbe.CountLocationsAsync(Fixture));
    }

    [Fact]
    public async Task CancelledTransaction_LeavesNothingBehind()
    {
        using var cts = new CancellationTokenSource();

        await using (AsyncServiceScope scope = NewScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var locations = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

            await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync(cts.Token);
            await locations.CreateOrGetAsync(new Coordinate(44.0, 44.0), cts.Token);

            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transaction.CommitAsync(cts.Token));
        }

        Assert.Equal(0, await CancellationProbe.CountLocationsAsync(Fixture));
    }
}