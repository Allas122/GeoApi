using System.Data.Common;
using System.Text.Json;
using Dapper;

namespace GeoApi.IntegrationTests.Api;

[Collection(GeoApiCollection.Name)]
public class QuerySemanticsTests(GeoApiFixture fixture) : ApiIntegrationTest(fixture)
{
    [Fact]
    public async Task Subtree_RespectsMaxDepthAndIncludeSelf()
    {
        await SeedAsync("root", "root.a", "root.a.b", "root.a.b.c");

        JsonElement withoutSelf = await GetAsync(
            "/resource/subtree?branchPath=root&maxDepth=1&includeSelf=false&limit=50");
        Assert.Equal(["root.a"], Branches(withoutSelf));

        JsonElement withSelf = await GetAsync(
            "/resource/subtree?branchPath=root&maxDepth=1&includeSelf=true&limit=50");
        Assert.Equal(["root", "root.a"], Branches(withSelf));

        JsonElement deep = await GetAsync("/resource/subtree?branchPath=root&includeSelf=false&limit=50");
        Assert.Equal(["root.a", "root.a.b", "root.a.b.c"], Branches(deep));
    }

    [Fact]
    public async Task Subtree_DefaultsToIncludingSelf()
    {
        await SeedAsync("root", "root.a");

        JsonElement page = await GetAsync("/resource/subtree?branchPath=root&limit=50");

        Assert.Contains("root", Branches(page));
    }

    [Fact]
    public async Task Ancestors_ReturnsTheChainFromRootToSelf()
    {
        await SeedAsync("a", "a.b", "a.b.c", "a.b.c.d", "unrelated");

        JsonElement page = await GetAsync("/resource/ancestors?branchPath=a.b.c.d&limit=50");

        Assert.Equal(["a", "a.b", "a.b.c", "a.b.c.d"], Branches(page));
    }

    [Fact]
    public async Task Search_MatchesLqueryWildcardsAndExcludesTheRest()
    {
        await SeedAsync("shop.moscow.food", "shop.spb.food", "office.moscow.food");

        JsonElement page = await GetAsync("/resource/search?pattern=shop.*.food&limit=50");

        Assert.Equal(["shop.moscow.food", "shop.spb.food"], Branches(page).Order().ToArray());
    }

    [Fact]
    public async Task Radius_ExcludesPointsOutsideTheCircle()
    {
        int near = await CreateLocationAsync(12.0, 13.0);
        int far = await CreateLocationAsync(13.0, 14.0);

        JsonElement tight = await GetAsync("/location/radius?longitude=12&latitude=13&radiusMeters=1000&limit=50");
        Assert.Equal([near], LocationIds(tight));

        JsonElement wide = await GetAsync("/location/radius?longitude=12&latitude=13&radiusMeters=500000&limit=50");
        Assert.Equal([near, far], LocationIds(wide).Order().ToArray());
    }

    [Fact]
    public async Task Clusters_AreFilteredByBranchPath()
    {
        await SeedAsync(
            ("moscow.center.cafe", ApiJson.Point(37.6208, 55.7539)),
            ("moscow.park.bench", ApiJson.Point(37.6740, 55.7890)));

        JsonElement all = await GetAsync(MoscowWindow());
        Assert.Equal(2, all.GetArrayLength());

        JsonElement filtered = await GetAsync($"{MoscowWindow()}&branchPath=moscow.center");
        Assert.Equal(1, filtered.GetArrayLength());
        Assert.Equal(1, filtered[0].GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Clusters_HideExpiredResourcesUnlessRequested()
    {
        await SeedAsync(("moscow.alive", ApiJson.Point(37.6740, 55.7890)));
        await SeedExpiredAsync("moscow.expired", ApiJson.Point(37.6208, 55.7539));

        JsonElement visible = await GetAsync(MoscowWindow());
        Assert.Equal(1, visible.GetArrayLength());

        JsonElement withExpired = await GetAsync($"{MoscowWindow()}&includeExpired=true");
        Assert.Equal(2, withExpired.GetArrayLength());
    }

    [Fact]
    public async Task Page_HidesExpiredResourcesUnlessRequested()
    {
        await SeedAsync("page.alive");
        await SeedExpiredAsync("page.expired", ApiJson.Point(20.0, 20.0));

        Assert.Equal(["page.alive"], Branches(await GetAsync("/resource?limit=50")));

        Assert.Equal(
            ["page.alive", "page.expired"],
            Branches(await GetAsync("/resource?limit=50&includeExpired=true")).Order().ToArray());
    }

    [Fact]
    public async Task Subtree_HidesExpiredResourcesUnlessRequested()
    {
        await SeedAsync("tree", "tree.alive");
        await SeedExpiredAsync("tree.expired", ApiJson.Point(21.0, 21.0));

        Assert.DoesNotContain(
            "tree.expired",
            Branches(await GetAsync("/resource/subtree?branchPath=tree&limit=50")));

        Assert.Contains(
            "tree.expired",
            Branches(await GetAsync("/resource/subtree?branchPath=tree&limit=50&includeExpired=true")));
    }

    [Fact]
    public async Task Search_HidesExpiredResourcesUnlessRequested()
    {
        await SeedAsync("find.alive");
        await SeedExpiredAsync("find.expired", ApiJson.Point(22.0, 22.0));

        Assert.Equal(
            ["find.alive"],
            Branches(await GetAsync("/resource/search?pattern=find.*&limit=50")));

        Assert.Equal(2, Branches(await GetAsync("/resource/search?pattern=find.*&limit=50&includeExpired=true")).Length);
    }

    [Fact]
    public async Task ByIds_IgnoresUnknownIdsAndHonoursIncludeExpired()
    {
        int[] alive = await SeedAsync("ids.alive");
        int expired = await SeedExpiredAsync("ids.expired", ApiJson.Point(23.0, 23.0));

        JsonElement found = await GetAsync($"/resource/by-ids?ids={alive[0]}&ids={expired}&ids=999999");
        Assert.Equal(
            ["ids.alive"],
            found.EnumerateArray().Select(r => r.GetProperty("resourceBranch").GetString()!).ToArray());

        JsonElement withExpired = await GetAsync(
            $"/resource/by-ids?ids={alive[0]}&ids={expired}&ids=999999&includeExpired=true");
        Assert.Equal(2, withExpired.GetArrayLength());
    }

    [Fact]
    public async Task ByLocation_ReturnsOnlyResourcesLinkedToThatLocation()
    {
        var shared = ApiJson.Point(41.0, 41.0);

        await SeedAsync(("link.first", shared), ("link.second", shared));
        await SeedAsync(("link.elsewhere", ApiJson.Point(42.0, 42.0)));

        int locationId = LocationIds(
            await GetAsync("/location/radius?longitude=41&latitude=41&radiusMeters=100&limit=10"))[0];

        JsonElement page = await GetAsync($"/resource/by-location/{locationId}?limit=50");

        Assert.Equal(["link.first", "link.second"], Branches(page).Order().ToArray());
    }

    [Fact]
    public async Task ResourceLocations_HonourLimitAndReturnOnlyItsOwnLocations()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch(
                "own.locations",
                0,
                ApiJson.Point(51.0, 51.0),
                ApiJson.Point(52.0, 52.0),
                ApiJson.Point(53.0, 53.0)));
        created.EnsureSuccessStatusCode();

        JsonElement batch = await ApiJson.ReadAsync(created);
        int resourceId = batch[0].GetProperty("resourceId").GetInt32();
        int[] expected = batch[0].GetProperty("locationIds")
            .EnumerateArray()
            .Select(id => id.GetInt32())
            .Order()
            .ToArray();

        await CreateLocationAsync(60.0, 60.0);

        JsonElement page = await GetAsync($"/resource/{resourceId}/locations?limit=50");
        Assert.Equal(expected, LocationIds(page).Order().ToArray());

        JsonElement firstOnly = await GetAsync($"/resource/{resourceId}/locations?limit=1");
        Assert.Single(LocationIds(firstOnly));
        Assert.True(firstOnly.GetProperty("hasMore").GetBoolean());
    }

    private static string MoscowWindow()
    {
        return "/location/clusters?minLon=37.4&minLat=55.6&maxLon=37.8&maxLat=55.9&gridSize=0.01";
    }

    private static string[] Branches(JsonElement page)
    {
        return page.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("resourceBranch").GetString()!)
            .ToArray();
    }

    private static int[] LocationIds(JsonElement page)
    {
        return page.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetInt32())
            .ToArray();
    }

    private async Task<JsonElement> GetAsync(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(url);
        Assert.True(response.IsSuccessStatusCode, $"{url} returned {(int)response.StatusCode}");
        return await ApiJson.ReadAsync(response);
    }

    private async Task<int> CreateLocationAsync(double longitude, double latitude)
    {
        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/location",
            new { point = ApiJson.Point(longitude, latitude) });

        response.EnsureSuccessStatusCode();
        return (await ApiJson.ReadAsync(response)).GetInt32();
    }

    private Task<int[]> SeedAsync(params string[] branches)
    {
        return SeedAsync(branches
            .Select((branch, index) => (branch, ApiJson.Point(1.0 + index, 1.0 + index)))
            .ToArray());
    }

    private async Task<int[]> SeedAsync(params (string Branch, object Point)[] resources)
    {
        HttpResponseMessage response = await ApiJson.PostAsync(Client, "/resource/batch", new
        {
            resources = resources
                .Select(resource => new
                {
                    resourceBranch = resource.Branch,
                    expiresInSeconds = 0L,
                    points = new[] { resource.Point }
                })
                .ToArray()
        });

        response.EnsureSuccessStatusCode();

        return (await ApiJson.ReadAsync(response))
            .EnumerateArray()
            .Select(resource => resource.GetProperty("resourceId").GetInt32())
            .ToArray();
    }

    private async Task<int> SeedExpiredAsync(string branch, object point)
    {
        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch(branch, 1, point));

        response.EnsureSuccessStatusCode();

        int resourceId = (await ApiJson.ReadAsync(response))[0].GetProperty("resourceId").GetInt32();

        await using DbConnection connection = await Fixture.OpenAsync();
        await connection.ExecuteAsync(
            "UPDATE public.resources SET created_at = now() - interval '1 hour' WHERE id = @resourceId",
            new { resourceId });

        return resourceId;
    }
}
