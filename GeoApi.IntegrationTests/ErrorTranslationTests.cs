using GeoApi.Domain.Geometry;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.ParameterObjects.Location;
using GeoApi.Domain.ParameterObjects.Resource;
using Npgsql;

namespace GeoApi.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class ErrorTranslationTests(PostgresFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task BrokenLquery_BecomesInvalidRequestException()
    {
        var exception = await Assert.ThrowsAsync<InvalidRequestException>(
            () => Resources.GetByBranchPatternAsync(new GetResourcesByBranchPatternParameters
            {
                Pattern = "***",
                Limit = 10
            }));

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.DoesNotContain("SQL", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BrokenLtree_BecomesInvalidRequestException()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Resources.GetAncestorsAsync(new GetResourceAncestorsParameters
            {
                BranchPath = "not a valid ltree",
                Limit = 10
            }));
    }

    [Fact]
    public async Task OverlongLtreeLabel_BecomesInvalidRequestException()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Resources.CreateAsync(new ResourceEntity
            {
                ResourceBranch = new string('a', 2000),
                ExpiresIn = TimeSpan.Zero
            }));
    }

    [Fact]
    public async Task LinkingMissingResource_BecomesConflictException()
    {
        int locationId = await Locations.CreateOrGetAsync(new Coordinate(1.0, 1.0));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => Resources.LinkLocationAsync(999_999, locationId));

        Assert.IsType<PostgresException>(exception.InnerException);
    }

    [Fact]
    public async Task UpdatingMissingLocation_ThrowsLocationNotFound()
    {
        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(
            () => Locations.UpdateAsync(new UpdateLocationParameters
            {
                Id = 999_999,
                Point = new Coordinate(1.0, 1.0)
            }));

        Assert.Equal(999_999, exception.LocationId);
    }

    [Fact]
    public async Task MovingLocationOntoOccupiedPoint_ThrowsConflictWithExistingId()
    {
        int first = await Locations.CreateOrGetAsync(new Coordinate(10.0, 20.0));
        int second = await Locations.CreateOrGetAsync(new Coordinate(11.0, 21.0));

        var exception = await Assert.ThrowsAsync<LocationPointConflictException>(
            () => Locations.UpdateAsync(new UpdateLocationParameters
            {
                Id = second,
                Point = new Coordinate(10.0, 20.0)
            }));

        Assert.Equal(first, exception.ExistingLocationId);
    }

    [Fact]
    public async Task MovingLocationToFreePoint_Succeeds()
    {
        int id = await Locations.CreateOrGetAsync(new Coordinate(10.0, 20.0));

        LocationEntity moved = await Locations.UpdateAsync(new UpdateLocationParameters
        {
            Id = id,
            Point = new Coordinate(12.0, 22.0)
        });

        Assert.Equal(id, moved.Id);
        Assert.Equal(12.0, moved.Point.Longitude, 6);
        Assert.Equal(22.0, moved.Point.Latitude, 6);
    }
}
