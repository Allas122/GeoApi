using System.Data.Common;
using Dapper;
using GeoApi.Domain.Dto.Location;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Location;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class ClusterTests(PostgresFixture fixture) : IntegrationTest(fixture)
{
    private static GetWindowedAndClusteredByGridParameters MoscowWindow(double gridSize = 0.01)
    {
        return new GetWindowedAndClusteredByGridParameters
        {
            MinLon = 37.4,
            MinLat = 55.6,
            MaxLon = 37.8,
            MaxLat = 55.9,
            GridSize = gridSize
        };
    }

    private async Task<int> SeedAsync(string branch, TimeSpan expiresIn, params Coordinate[] points)
    {
        int resourceId = await Resources.CreateAsync(new ResourceEntity
        {
            ResourceBranch = branch,
            ExpiresIn = expiresIn
        });

        int[] locationIds = (await Locations.BulkCreateOrGetAsync(points)).ToArray();
        await Resources.BulkLinkLocationsAsync(resourceId, locationIds);
        return resourceId;
    }

    [Fact]
    public async Task Centroid_IsNotSkewedBySharedLocations()
    {
        var shared = new Coordinate(37.6208, 55.7539);
        await SeedAsync("moscow.center.cafe", TimeSpan.Zero, shared, new Coordinate(37.6216, 55.7542));
        await SeedAsync("moscow.center.museum", TimeSpan.Zero, new Coordinate(37.6231, 55.7528), shared);

        GridClusterWithResourceIds cluster =
            Assert.Single(await Locations.GetWindowedAndClusteredByGridAsync(MoscowWindow()));

        Assert.Equal(3, cluster.Count);
        Assert.Equal(2, cluster.ResourceCount);
        Assert.Equal(37.6218333, cluster.Center.Longitude, 6);
        Assert.Equal(55.7536333, cluster.Center.Latitude, 6);
    }

    [Fact]
    public async Task Clusters_SplitByGridCell()
    {
        await SeedAsync("moscow.city", TimeSpan.Zero, new Coordinate(37.5372, 55.7497), new Coordinate(37.5390, 55.7503));
        await SeedAsync("moscow.park", TimeSpan.Zero, new Coordinate(37.6740, 55.7890));

        GridClusterWithResourceIds[] clusters =
            (await Locations.GetWindowedAndClusteredByGridAsync(MoscowWindow())).ToArray();

        Assert.Equal(2, clusters.Length);
        Assert.True(clusters[0].Center.Longitude < clusters[1].Center.Longitude);
    }

    [Fact]
    public async Task Window_ExcludesPointsOutside()
    {
        await SeedAsync("moscow.center", TimeSpan.Zero, new Coordinate(37.6208, 55.7539));
        await SeedAsync("spb.center", TimeSpan.Zero, new Coordinate(30.3141, 59.9386));

        GridClusterWithResourceIds cluster =
            Assert.Single(await Locations.GetWindowedAndClusteredByGridAsync(MoscowWindow()));

        Assert.Equal(1, cluster.Count);
    }

    [Fact]
    public async Task BranchPath_FiltersClusters()
    {
        await SeedAsync("moscow.center.cafe", TimeSpan.Zero, new Coordinate(37.6208, 55.7539));
        await SeedAsync("moscow.park.bench", TimeSpan.Zero, new Coordinate(37.6740, 55.7890));

        GetWindowedAndClusteredByGridParameters parameters = MoscowWindow();
        parameters.BranchPath = "moscow.center";

        GridClusterWithResourceIds cluster =
            Assert.Single(await Locations.GetWindowedAndClusteredByGridAsync(parameters));

        Assert.Equal(1, cluster.Count);
    }

    [Fact]
    public async Task ExpiredResources_AreHiddenFromClustersByDefault()
    {
        int expiring = await SeedAsync(
            "moscow.expiring",
            TimeSpan.FromSeconds(1),
            new Coordinate(37.5372, 55.7497));
        await SeedAsync("moscow.alive", TimeSpan.Zero, new Coordinate(37.6740, 55.7890));

        await using (DbConnection connection = await Fixture.OpenAsync())
        {
            await connection.ExecuteAsync(
                "UPDATE public.resources SET created_at = now() - interval '1 hour' WHERE id = @expiring",
                new { expiring });
        }

        GridClusterWithResourceIds visible =
            Assert.Single(await Locations.GetWindowedAndClusteredByGridAsync(MoscowWindow()));
        Assert.DoesNotContain(expiring, visible.ResourceIds);

        GetWindowedAndClusteredByGridParameters withExpired = MoscowWindow();
        withExpired.IncludeExpired = true;

        GridClusterWithResourceIds[] all =
            (await Locations.GetWindowedAndClusteredByGridAsync(withExpired)).ToArray();
        Assert.Equal(2, all.Length);
    }

    [Fact]
    public async Task StandaloneLocations_NeverAppearInClusters()
    {
        await Locations.CreateOrGetAsync(new Coordinate(37.6208, 55.7539));

        Assert.Empty(await Locations.GetWindowedAndClusteredByGridAsync(MoscowWindow()));
    }
}
