using System.Net;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public class BulkEndpointsTests(PostgresFixture fixture) : ApiIntegrationTest(fixture)
{
    [Fact]
    public async Task CreateLocationBatch_ReturnsIdsInInputOrderAndReusesRepeatedPoints()
    {
        HttpResponseMessage response = await ApiJson.PostAsync(Client, "/location/batch", new
        {
            points = new[]
            {
                ApiJson.Point(10.0, 20.0),
                ApiJson.Point(11.0, 21.0),
                ApiJson.Point(10.0, 20.0)
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement ids = await ApiJson.ReadAsync(response);
        Assert.Equal(3, ids.GetArrayLength());
        Assert.Equal(ids[0].GetInt32(), ids[2].GetInt32());
        Assert.NotEqual(ids[0].GetInt32(), ids[1].GetInt32());
    }

    [Fact]
    public async Task CreateLocationBatch_IsIdempotentAcrossCalls()
    {
        object body = new { points = new[] { ApiJson.Point(30.5, 50.4) } };

        JsonElement first = await ApiJson.ReadAsync(await ApiJson.PostAsync(Client, "/location/batch", body));
        JsonElement second = await ApiJson.ReadAsync(await ApiJson.PostAsync(Client, "/location/batch", body));

        Assert.Equal(first[0].GetInt32(), second[0].GetInt32());
    }

    [Fact]
    public async Task CreateLocationBatch_WithoutPoints_ReturnsBadRequest()
    {
        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/location/batch",
            new { points = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateLocationBatch_WithOutOfRangePoint_ReturnsBadRequest()
    {
        HttpResponseMessage response = await ApiJson.PostAsync(Client, "/location/batch", new
        {
            points = new[] { ApiJson.Point(10.0, 20.0), ApiJson.Point(200.0, 20.0) }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkLocations_RemovesOnlyTheRequestedLinks()
    {
        (int resourceId, int[] locationIds) = await CreateResourceWithLocationsAsync("bulk.unlink");

        HttpResponseMessage response = await Client.DeleteAsync(
            $"/resource/{resourceId}/locations?locationIds={locationIds[0]}&locationIds={locationIds[1]}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        int[] unlinked = (await ApiJson.ReadAsync(response))
            .EnumerateArray()
            .Select(id => id.GetInt32())
            .Order()
            .ToArray();

        Assert.Equal(new[] { locationIds[0], locationIds[1] }.Order().ToArray(), unlinked);

        JsonElement remaining = await ApiJson.ReadAsync(
            await Client.GetAsync($"/resource/{resourceId}/locations?limit=10"));

        Assert.Equal(1, remaining.GetProperty("items").GetArrayLength());
        Assert.Equal(locationIds[2], remaining.GetProperty("items")[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task UnlinkLocations_IgnoresLinksThatDoNotExist()
    {
        (int resourceId, int[] locationIds) = await CreateResourceWithLocationsAsync("bulk.unlink.absent");

        HttpResponseMessage response = await Client.DeleteAsync(
            $"/resource/{resourceId}/locations?locationIds={locationIds[0]}&locationIds=999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement unlinked = await ApiJson.ReadAsync(response);
        Assert.Equal(1, unlinked.GetArrayLength());
        Assert.Equal(locationIds[0], unlinked[0].GetInt32());
    }

    [Fact]
    public async Task UnlinkLocations_ForMissingResource_ReturnsNotFound()
    {
        HttpResponseMessage response = await Client.DeleteAsync("/resource/999999/locations?locationIds=1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?locationIds=0")]
    [InlineData("?locationIds=-3")]
    public async Task UnlinkLocations_WithInvalidIds_ReturnsBadRequest(string query)
    {
        (int resourceId, _) = await CreateResourceWithLocationsAsync("bulk.unlink.invalid");

        HttpResponseMessage response = await Client.DeleteAsync($"/resource/{resourceId}/locations{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(int ResourceId, int[] LocationIds)> CreateResourceWithLocationsAsync(string branch)
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch(
                branch,
                0,
                ApiJson.Point(1.0, 1.0),
                ApiJson.Point(2.0, 2.0),
                ApiJson.Point(3.0, 3.0)));

        created.EnsureSuccessStatusCode();

        JsonElement batch = await ApiJson.ReadAsync(created);
        int resourceId = batch[0].GetProperty("resourceId").GetInt32();
        int[] locationIds = batch[0].GetProperty("locationIds")
            .EnumerateArray()
            .Select(id => id.GetInt32())
            .ToArray();

        return (resourceId, locationIds);
    }
}
