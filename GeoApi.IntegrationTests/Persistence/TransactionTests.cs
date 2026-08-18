using System.Data.Common;
using Dapper;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Database.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(GeoApiCollection.Name)]
public class TransactionTests(GeoApiFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Transaction_RollsBackWhenScopeIsDisposedWithoutCommit()
    {
        int resourceId;

        await using (AsyncServiceScope scope = NewScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();

            await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync();
            resourceId = await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = "rollback.me",
                ExpiresIn = TimeSpan.Zero
            });
        }

        Assert.Null(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = resourceId }));
    }

    [Fact]
    public async Task Transaction_PersistsAfterCommit()
    {
        int resourceId;

        await using (AsyncServiceScope scope = NewScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();

            await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync();
            resourceId = await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = "commit.me",
                ExpiresIn = TimeSpan.Zero
            });
            await transaction.CommitAsync();
        }

        Assert.NotNull(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = resourceId }));
    }

    [Fact]
    public async Task Transaction_ExplicitRollbackDiscardsChangesAndClosesTheScope()
    {
        int resourceId;

        await using (AsyncServiceScope scope = NewScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();

            await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync();
            resourceId = await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = "explicit.rollback",
                ExpiresIn = TimeSpan.Zero
            });

            await transaction.RollbackAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        }

        Assert.Null(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = resourceId }));
    }

    [Fact]
    public async Task Transaction_CannotBeStartedTwiceInOneScope()
    {
        await using AsyncServiceScope scope = NewScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task Transaction_RollsBackEveryRepositoryWhenScopeIsDisposedWithoutCommit()
    {
        await using (AsyncServiceScope scope = NewScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
            var locations = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

            await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync();

            int resourceId = await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = "shared.tx",
                ExpiresIn = TimeSpan.Zero
            });
            int[] locationIds = (await locations.BulkCreateOrGetAsync([new Coordinate(5.0, 5.0)])).ToArray();
            await resources.BulkLinkPairsAsync([resourceId], locationIds);
        }

        Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM public.locations"));
        Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM public.resources"));
        Assert.Equal(0, await CountAsync("SELECT COUNT(*) FROM public.resource_location"));
    }

    [Fact]
    public async Task Transaction_CommitsEveryRepositoryAsOneUnit()
    {
        int resourceId;
        int locationId;

        await using (AsyncServiceScope scope = NewScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
            var locations = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

            await using ITransactionScope transaction = await unitOfWork.BeginTransactionAsync();

            resourceId = await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = "shared.commit",
                ExpiresIn = TimeSpan.Zero
            });
            int[] locationIds = (await locations.BulkCreateOrGetAsync([new Coordinate(6.0, 6.0)])).ToArray();
            locationId = locationIds[0];
            await resources.BulkLinkPairsAsync([resourceId], locationIds);

            await transaction.CommitAsync();
        }

        Assert.NotNull(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = resourceId }));
        Assert.NotNull(await Locations.GetByIdAsync(locationId));

        int[] linked = (await Resources.GetLocationsAsync(new GetResourceLocationsByIdParameters
        {
            ResourceId = resourceId,
            Limit = 10
        })).Select(location => location.Id).ToArray();

        Assert.Equal([locationId], linked);
    }

    [Fact]
    public async Task DeleteAsync_RemovesResourceAndItsLinks()
    {
        int resourceId = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "delete.me",
            ExpiresIn = TimeSpan.Zero
        });
        int[] locationIds = (await Locations.BulkCreateOrGetAsync([new Coordinate(7.0, 7.0)])).ToArray();
        await Resources.BulkLinkPairsAsync([resourceId], locationIds);

        Assert.True(await Resources.DeleteAsync(resourceId));
        Assert.False(await Resources.DeleteAsync(resourceId));
        Assert.Null(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = resourceId }));

        await using DbConnection connection = await Fixture.OpenAsync();
        int links = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM public.resource_location WHERE resource_id = @resourceId",
            new { resourceId });

        Assert.Equal(0, links);
    }

    [Fact]
    public async Task SequentialCallsInOneScope_ShareASingleConnection()
    {
        await using AsyncServiceScope scope = NewScope();
        var session = scope.ServiceProvider.GetRequiredService<IDbSession>();
        var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
        var locations = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        var backendPids = new List<int>();

        for (int i = 0; i < 10; i++)
        {
            await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = $"reuse.n{i}",
                ExpiresIn = TimeSpan.Zero
            });
            await locations.CreateOrGetAsync(new Coordinate(i, i));

            await using DbConnectionLease lease = await session.LeaseAsync();
            backendPids.Add(await lease.Connection.ExecuteScalarAsync<int>("SELECT pg_backend_pid()"));
        }

        Assert.Equal(10, backendPids.Count);
        Assert.Single(backendPids.Distinct());
    }

    [Fact]
    public async Task SeparateScopes_DoNotShareAConnection()
    {
        await using AsyncServiceScope first = NewScope();
        await using AsyncServiceScope second = NewScope();

        int firstPid = await BackendPidAsync(first);
        int secondPid = await BackendPidAsync(second);

        Assert.NotEqual(firstPid, secondPid);
    }

    private static async Task<int> BackendPidAsync(AsyncServiceScope scope)
    {
        var session = scope.ServiceProvider.GetRequiredService<IDbSession>();
        await using DbConnectionLease lease = await session.LeaseAsync();
        return await lease.Connection.ExecuteScalarAsync<int>("SELECT pg_backend_pid()");
    }

    private async Task<int> CountAsync(string sql)
    {
        await using DbConnection connection = await Fixture.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
