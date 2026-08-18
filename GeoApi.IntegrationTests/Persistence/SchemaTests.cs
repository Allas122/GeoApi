using System.Data.Common;
using Dapper;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(GeoApiCollection.Name)]
public class SchemaTests(GeoApiFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task Timestamps_AreStoredWithTimeZone()
    {
        await using DbConnection connection = await Fixture.OpenAsync();

        string[] types = (await connection.QueryAsync<string>(
            """
            SELECT data_type FROM information_schema.columns
            WHERE table_name = 'resources' AND column_name IN ('created_at', 'updated_at')
            """)).ToArray();

        Assert.Equal(2, types.Length);
        Assert.All(types, type => Assert.Equal("timestamp with time zone", type));
    }

    [Fact]
    public async Task UpdatedAt_IsMovedByTrigger_WithoutApplicationWritingIt()
    {
        int id = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "root.trigger",
            ExpiresIn = TimeSpan.Zero
        });

        ResourceEntity created = await GetAsync(id);
        await Task.Delay(20);

        ResourceEntity? updated = await Resources.UpdateAsync(new UpdateResourceParameters
        {
            Id = id,
            ResourceBranch = "root.trigger.moved",
            ExpiresIn = TimeSpan.Zero
        });

        Assert.NotNull(updated);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);
    }

    [Fact]
    public async Task ExpiredResource_DisappearsUnlessIncludeExpiredIsSet()
    {
        int id = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "root.expiring",
            ExpiresIn = TimeSpan.FromSeconds(1)
        });

        await using (DbConnection connection = await Fixture.OpenAsync())
        {
            await connection.ExecuteAsync(
                "UPDATE public.resources SET created_at = now() - interval '1 hour' WHERE id = @id",
                new { id });
        }

        Assert.Null(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = id }));
        Assert.NotNull(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = id, IncludeExpired = true }));
    }

    [Fact]
    public async Task ZeroExpiry_NeverExpires()
    {
        int id = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "root.eternal",
            ExpiresIn = TimeSpan.Zero
        });

        await using (DbConnection connection = await Fixture.OpenAsync())
        {
            await connection.ExecuteAsync(
                "UPDATE public.resources SET created_at = now() - interval '100 years' WHERE id = @id",
                new { id });
        }

        Assert.NotNull(await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = id }));
    }

    private async Task<ResourceEntity> GetAsync(int id)
    {
        ResourceEntity? resource = await Resources.GetByIdAsync(
            new GetResourceByIdParameters { Id = id, IncludeExpired = true });
        Assert.NotNull(resource);
        return resource;
    }
}
