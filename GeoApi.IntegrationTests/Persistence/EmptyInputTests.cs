using GeoApi.Domain.Entities;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.ParameterObjects.Resource;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class EmptyInputTests(PostgresFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task BulkCreateOrGetAsync_WithNoPoints_ReturnsEmpty()
    {
        Assert.Empty(await Locations.BulkCreateOrGetAsync([]));
    }

    [Fact]
    public async Task BulkCreateAsync_WithNoResources_ReturnsEmpty()
    {
        Assert.Empty(await Resources.BulkCreateAsync([]));
    }

    [Fact]
    public async Task GetByIdsAsync_WithNoIds_ReturnsEmpty()
    {
        Assert.Empty(await Resources.GetByIdsAsync(new GetResourcesByIdsParameters { Ids = [] }));
    }

    [Fact]
    public async Task BulkLinkLocationsAsync_WithNoLocations_ReturnsEmpty()
    {
        int resourceId = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "empty.link",
            ExpiresIn = TimeSpan.Zero
        });

        Assert.Empty(await Resources.BulkLinkLocationsAsync(resourceId, []));
    }

    [Fact]
    public async Task BulkLinkPairsAsync_WithNoPairs_LinksNothing()
    {
        Assert.Equal(0, await Resources.BulkLinkPairsAsync([], []));
    }

    [Fact]
    public async Task BulkLinkPairsAsync_WithMismatchedLengths_Throws()
    {
        int resourceId = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = "mismatched.pairs",
            ExpiresIn = TimeSpan.Zero
        });
        int locationId = await Locations.CreateOrGetAsync(new Coordinate(1.0, 1.0));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => Resources.BulkLinkPairsAsync([resourceId, resourceId], [locationId]));

        Assert.Equal("locationIds", exception.ParamName);
    }
}
