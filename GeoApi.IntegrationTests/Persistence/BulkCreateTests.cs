using GeoApi.Domain.Geometry;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(GeoApiCollection.Name)]
public class BulkCreateTests(GeoApiFixture fixture) : IntegrationTest(fixture)
{
    [Fact]
    public async Task BulkCreateAsync_ReturnsIdsInInputOrder()
    {
        string[] branches = ["z.last", "a.first", "m.middle", "b.second"];

        IReadOnlyList<int> ids = await Resources.BulkCreateAsync(
            branches.Select(branch => new ResourceEntity { ResourceBranch = branch, ExpiresIn = TimeSpan.Zero })
                .ToArray());

        Assert.Equal(branches.Length, ids.Count);

        for (int i = 0; i < branches.Length; i++)
        {
            ResourceEntity? stored = await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = ids[i] });
            Assert.NotNull(stored);
            Assert.Equal(branches[i], stored.ResourceBranch);
        }
    }

    [Fact]
    public async Task BulkCreateAsync_AllocatesEachIdExactlyOnce()
    {
        IReadOnlyList<int> ids = await Resources.BulkCreateAsync(
            Enumerable.Range(0, 50)
                .Select(i => new ResourceEntity { ResourceBranch = $"batch.n{i}", ExpiresIn = TimeSpan.Zero })
                .ToArray());

        Assert.Equal(50, ids.Count);
        Assert.Equal(50, ids.Distinct().Count());
    }

    [Fact]
    public async Task BulkCreateAsync_KeepsOrderForDuplicateBranches()
    {
        IReadOnlyList<int> ids = await Resources.BulkCreateAsync([
            new ResourceEntity { ResourceBranch = "same.branch", ExpiresIn = TimeSpan.Zero },
            new ResourceEntity { ResourceBranch = "same.branch", ExpiresIn = TimeSpan.FromMinutes(5) },
            new ResourceEntity { ResourceBranch = "same.branch", ExpiresIn = TimeSpan.FromMinutes(10) }
        ]);

        TimeSpan[] stored = [];
        foreach (int id in ids)
        {
            ResourceEntity? resource = await Resources.GetByIdAsync(new GetResourceByIdParameters { Id = id });
            Assert.NotNull(resource);
            stored = [.. stored, resource.ExpiresIn];
        }

        Assert.Equal([TimeSpan.Zero, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10)], stored);
    }

    [Fact]
    public async Task BulkCreateOrGetAsync_ReturnsSameIdForRepeatedPoint()
    {
        var point = new Coordinate(37.6208, 55.7539);

        int[] first = (await Locations.BulkCreateOrGetAsync([point, new Coordinate(37.6216, 55.7542)])).ToArray();
        int[] second = (await Locations.BulkCreateOrGetAsync([new Coordinate(37.6216, 55.7542), point])).ToArray();

        Assert.Equal(first[0], second[1]);
        Assert.Equal(first[1], second[0]);
    }

    [Fact]
    public async Task BulkCreateOrGetAsync_KeepsInputOrderWithDuplicatesInside()
    {
        var a = new Coordinate(10.0, 20.0);
        var b = new Coordinate(11.0, 21.0);

        int[] ids = (await Locations.BulkCreateOrGetAsync([a, b, a, b, a])).ToArray();

        Assert.Equal(5, ids.Length);
        Assert.Equal(ids[0], ids[2]);
        Assert.Equal(ids[0], ids[4]);
        Assert.Equal(ids[1], ids[3]);
        Assert.NotEqual(ids[0], ids[1]);
    }

    [Fact]
    public async Task CreateOrGetAsync_IsIdempotent()
    {
        var point = new Coordinate(30.5, 50.4);

        int first = await Locations.CreateOrGetAsync(point);
        int second = await Locations.CreateOrGetAsync(point);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task BulkLinkPairsAsync_LinksEachResourceToItsOwnLocations()
    {
        IReadOnlyList<int> resourceIds = await Resources.BulkCreateAsync([
            new ResourceEntity { ResourceBranch = "pairs.a", ExpiresIn = TimeSpan.Zero },
            new ResourceEntity { ResourceBranch = "pairs.b", ExpiresIn = TimeSpan.Zero }
        ]);

        int[] locationIds = (await Locations.BulkCreateOrGetAsync([
            new Coordinate(1.0, 1.0),
            new Coordinate(2.0, 2.0),
            new Coordinate(3.0, 3.0)
        ])).ToArray();

        await Resources.BulkLinkPairsAsync(
            [resourceIds[0], resourceIds[1], resourceIds[1]],
            [locationIds[0], locationIds[1], locationIds[2]]);

        int[] firstLocations = (await Resources.GetLocationsAsync(new GetResourceLocationsByIdParameters
        {
            ResourceId = resourceIds[0],
            Limit = 10
        })).Select(location => location.Id).ToArray();

        int[] secondLocations = (await Resources.GetLocationsAsync(new GetResourceLocationsByIdParameters
        {
            ResourceId = resourceIds[1],
            Limit = 10
        })).Select(location => location.Id).ToArray();

        Assert.Equal([locationIds[0]], firstLocations);
        Assert.Equal([locationIds[1], locationIds[2]], secondLocations.Order().ToArray());
    }
}
