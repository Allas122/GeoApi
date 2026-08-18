using System.Net;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(GeoApiCollection.Name)]
public class ErrorContractTests(GeoApiFixture fixture) : ApiIntegrationTest(fixture)
{
    private async Task<int> CreateLocationAsync(double longitude, double latitude)
    {
        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/location",
            new { point = ApiJson.Point(longitude, latitude) });

        response.EnsureSuccessStatusCode();
        return (await ApiJson.ReadAsync(response)).GetInt32();
    }

    [Fact]
    public async Task MissingResource_ReturnsProblemDetailsWithTraceId()
    {
        HttpResponseMessage response = await Client.GetAsync("/resource/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        JsonElement problem = await ApiJson.ReadAsync(response);
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.Contains("999999", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task MissingLocation_ReturnsNotFound()
    {
        HttpResponseMessage response = await Client.GetAsync("/location/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletingMissingResource_ReturnsNotFound()
    {
        HttpResponseMessage response = await Client.DeleteAsync("/resource/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MovingLocationOntoOccupiedPoint_ReturnsConflictWithExistingId()
    {
        int occupied = await CreateLocationAsync(10.0, 20.0);
        int moving = await CreateLocationAsync(11.0, 21.0);

        HttpResponseMessage response = await ApiJson.PutAsync(
            Client,
            $"/location/{moving}",
            new { point = ApiJson.Point(10.0, 20.0) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        JsonElement problem = await ApiJson.ReadAsync(response);
        Assert.Equal(occupied, problem.GetProperty("existingLocationId").GetInt32());
    }

    [Fact]
    public async Task LinkingMissingLocation_ReturnsNotFound()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("root.linktest", 0, ApiJson.Point(1, 1)));
        created.EnsureSuccessStatusCode();

        int resourceId = (await ApiJson.ReadAsync(created))[0].GetProperty("resourceId").GetInt32();

        HttpResponseMessage response = await Client.PostAsync($"/resource/{resourceId}/locations/999999", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnlinkingAbsentLink_ReturnsNotFound()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("root.unlinktest", 0, ApiJson.Point(2, 2)));
        created.EnsureSuccessStatusCode();

        int resourceId = (await ApiJson.ReadAsync(created))[0].GetProperty("resourceId").GetInt32();
        int otherLocation = await CreateLocationAsync(50.0, 50.0);

        HttpResponseMessage response =
            await Client.DeleteAsync($"/resource/{resourceId}/locations/{otherLocation}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/resource/999999")]
    [InlineData("/location/999999")]
    [InlineData("/resource?limit=99999")]
    [InlineData("/resource/search?pattern=***")]
    public async Task ErrorBodies_DoNotLeakDatabaseInternals(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True((int)response.StatusCode >= 400);
        Assert.DoesNotContain("postgres", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sqlstate", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("public.resources", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MovingLocationToFreePoint_ReturnsUpdatedCoordinates()
    {
        int locationId = await CreateLocationAsync(12.0, 22.0);

        HttpResponseMessage response = await ApiJson.PutAsync(
            Client,
            $"/location/{locationId}",
            new { point = ApiJson.Point(13.5, 23.5) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement updated = await ApiJson.ReadAsync(response);
        Assert.Equal(locationId, updated.GetProperty("id").GetInt32());
        Assert.Equal(13.5, updated.GetProperty("point").GetProperty("longitude").GetDouble(), 6);
        Assert.Equal(23.5, updated.GetProperty("point").GetProperty("latitude").GetDouble(), 6);
    }

    [Fact]
    public async Task Health_ReportsHealthyWithLiveDatabase()
    {
        HttpResponseMessage response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
