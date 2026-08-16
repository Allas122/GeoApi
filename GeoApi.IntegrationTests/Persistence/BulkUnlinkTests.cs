using GeoApi.Domain.Entities;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.ParameterObjects.Resource;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class BulkUnlinkTests(PostgresFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task BulkUnlinkLocationsAsync_ReturnsRemovedIdsAndKeepsTheRest()
    {
        (int resourceId, int[] locationIds) = await CreateLinkedResourceAsync("unlink.some");

        int[] removed = (await Resources.BulkUnlinkLocationsAsync(resourceId, [locationIds[0], locationIds[2]]))
            .Order()
            .ToArray();

        Assert.Equal(new[] { locationIds[0], locationIds[2] }.Order().ToArray(), removed);
        Assert.Equal([locationIds[1]], await RemainingLocationsAsync(resourceId));
    }

    [Fact]
    public async Task BulkUnlinkLocationsAsync_WithEmptyListTouchesNothing()
    {
        (int resourceId, int[] locationIds) = await CreateLinkedResourceAsync("unlink.none");

        Assert.Empty(await Resources.BulkUnlinkLocationsAsync(resourceId, []));
        Assert.Equal(locationIds.Order().ToArray(), await RemainingLocationsAsync(resourceId));
    }

    [Fact]
    public async Task BulkUnlinkLocationsAsync_DoesNotTouchOtherResources()
    {
        (int first, int[] locationIds) = await CreateLinkedResourceAsync("unlink.first");

        int second = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "unlink.second",
            ExpiresIn = TimeSpan.Zero
        });
        await Resources.BulkLinkPairsAsync([second, second], [locationIds[0], locationIds[1]]);

        await Resources.BulkUnlinkLocationsAsync(first, locationIds);

        Assert.Empty(await RemainingLocationsAsync(first));
        Assert.Equal(
            new[] { locationIds[0], locationIds[1] }.Order().ToArray(),
            await RemainingLocationsAsync(second));
    }

    [Fact]
    public async Task BulkUnlinkLocationsAsync_IsIdempotent()
    {
        (int resourceId, int[] locationIds) = await CreateLinkedResourceAsync("unlink.twice");

        Assert.Single(await Resources.BulkUnlinkLocationsAsync(resourceId, [locationIds[0]]));
        Assert.Empty(await Resources.BulkUnlinkLocationsAsync(resourceId, [locationIds[0]]));
    }

    private async Task<int[]> RemainingLocationsAsync(int resourceId)
    {
        IEnumerable<LocationEntity> locations = await Resources.GetLocationsAsync(
            new GetResourceLocationsByIdParameters
            {
                ResourceId = resourceId,
                Limit = 100
            });

        return locations.Select(location => location.Id).Order().ToArray();
    }

    private async Task<(int ResourceId, int[] LocationIds)> CreateLinkedResourceAsync(string branch)
    {
        int resourceId = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = branch,
            ExpiresIn = TimeSpan.Zero
        });

        int[] locationIds = (await Locations.BulkCreateOrGetAsync([
            new Coordinate(1.0, 1.0),
            new Coordinate(2.0, 2.0),
            new Coordinate(3.0, 3.0)
        ])).ToArray();

        await Resources.BulkLinkPairsAsync(
            [resourceId, resourceId, resourceId],
            locationIds);

        return (resourceId, locationIds);
    }
}
