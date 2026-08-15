using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public class SerializationTests(PostgresFixture fixture) : ApiIntegrationTest(fixture)
{
    private async Task<int> SeedResourceAsync(string branch = "root.serialize", params object[] points)
    {
        object[] usedPoints = points.Length == 0 ? [ApiJson.Point(37.6208, 55.7539)] : points;

        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch(branch, 0, usedPoints));

        response.EnsureSuccessStatusCode();
        return (await ApiJson.ReadAsync(response))[0].GetProperty("resourceId").GetInt32();
    }

    [Fact]
    public async Task ResourceResponse_UsesCamelCaseNames()
    {
        int id = await SeedResourceAsync();

        JsonElement resource = await ApiJson.ReadAsync(await Client.GetAsync($"/resource/{id}"));

        string[] names = resource.EnumerateObject().Select(property => property.Name).ToArray();

        Assert.Equal(["id", "resourceBranch", "createdAt", "updatedAt", "expiresInSeconds"], names);
    }

    [Fact]
    public async Task Timestamps_AreSerializedWithUtcOffset()
    {
        int id = await SeedResourceAsync();

        JsonElement resource = await ApiJson.ReadAsync(await Client.GetAsync($"/resource/{id}"));

        string createdAt = resource.GetProperty("createdAt").GetString()!;

        Assert.EndsWith("+00:00", createdAt);
        Assert.Equal(TimeSpan.Zero, DateTimeOffset.Parse(createdAt).Offset);
    }

    [Fact]
    public async Task PagedResponse_HasCursorShape()
    {
        await SeedResourceAsync("root.paged.a");
        await SeedResourceAsync("root.paged.b", ApiJson.Point(37.7, 55.8));

        JsonElement page = await ApiJson.ReadAsync(await Client.GetAsync("/resource?limit=1"));

        Assert.True(page.TryGetProperty("items", out JsonElement items));
        Assert.True(page.TryGetProperty("nextLastId", out _));
        Assert.True(page.GetProperty("hasMore").GetBoolean());
        Assert.Equal(1, items.GetArrayLength());
    }

    [Fact]
    public async Task ClusterResponse_CarriesResourceCountBesideResourceIds()
    {
        await SeedResourceAsync("moscow.a", ApiJson.Point(37.6208, 55.7539));
        await SeedResourceAsync("moscow.b", ApiJson.Point(37.6216, 55.7542));

        JsonElement clusters = await ApiJson.ReadAsync(await Client.GetAsync(
            "/location/clusters?minLon=37.4&minLat=55.6&maxLon=37.8&maxLat=55.9&gridSize=0.01"));

        JsonElement cluster = clusters[0];

        Assert.Equal(2, cluster.GetProperty("count").GetInt32());
        Assert.Equal(2, cluster.GetProperty("resourceCount").GetInt32());
        Assert.Equal(2, cluster.GetProperty("resourceIds").GetArrayLength());
        Assert.True(cluster.GetProperty("center").TryGetProperty("longitude", out _));
    }

    [Fact]
    public async Task LocationResponse_UsesLongitudeLatitudeNames()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/location",
            new { point = ApiJson.Point(30.5, 50.4) });
        created.EnsureSuccessStatusCode();

        int id = (await ApiJson.ReadAsync(created)).GetInt32();

        JsonElement location = await ApiJson.ReadAsync(await Client.GetAsync($"/location/{id}"));

        Assert.Equal(id, location.GetProperty("id").GetInt32());
        Assert.Equal(30.5, location.GetProperty("point").GetProperty("longitude").GetDouble(), 6);
        Assert.Equal(50.4, location.GetProperty("point").GetProperty("latitude").GetDouble(), 6);
    }

    [Fact]
    public async Task ByIdsRoute_IsNotSwallowedByIdRoute()
    {
        int first = await SeedResourceAsync("root.byids.a");
        int second = await SeedResourceAsync("root.byids.b", ApiJson.Point(37.7, 55.8));

        JsonElement resources = await ApiJson.ReadAsync(
            await Client.GetAsync($"/resource/by-ids?ids={first}&ids={second}"));

        Assert.Equal(2, resources.GetArrayLength());
    }

    [Fact]
    public async Task ByLocationRoute_ReturnsPagedShape()
    {
        await SeedResourceAsync("root.bylocation", ApiJson.Point(12.0, 13.0));

        JsonElement locations = await ApiJson.ReadAsync(
            await Client.GetAsync("/location/radius?longitude=12&latitude=13&radiusMeters=1000&limit=10"));

        int locationId = locations.GetProperty("items")[0].GetProperty("id").GetInt32();

        JsonElement page = await ApiJson.ReadAsync(
            await Client.GetAsync($"/resource/by-location/{locationId}?limit=10"));

        Assert.True(page.TryGetProperty("items", out _));
        Assert.True(page.TryGetProperty("hasMore", out _));
    }
}
