using GeoApi.Domain.Geometry;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class TransactionTests(PostgresFixture fixture) : IntegrationTest(fixture)
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
    public async Task Transaction_SpansMultipleRepositories()
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

        await using System.Data.Common.DbConnection connection = await Fixture.OpenAsync();
        int locationCount = await Dapper.SqlMapper.ExecuteScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM public.locations");

        Assert.Equal(0, locationCount);
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

        await using System.Data.Common.DbConnection connection = await Fixture.OpenAsync();
        int links = await Dapper.SqlMapper.ExecuteScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM public.resource_location WHERE resource_id = @resourceId",
            new { resourceId });

        Assert.Equal(0, links);
    }

    [Fact]
    public async Task SequentialCallsInOneScope_ShareASingleConnection()
    {
        await using AsyncServiceScope scope = NewScope();
        var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
        var locations = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        for (int i = 0; i < 10; i++)
        {
            await resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = $"reuse.n{i}",
                ExpiresIn = TimeSpan.Zero
            });
            await locations.CreateOrGetAsync(new Coordinate(i, i));
        }

        await using System.Data.Common.DbConnection connection = await Fixture.OpenAsync();
        int backends = await Dapper.SqlMapper.ExecuteScalarAsync<int>(
            connection,
            "SELECT COUNT(*) FROM pg_stat_activity WHERE datname = current_database() AND state = 'idle'");

        Assert.True(backends < 10);
    }
}
