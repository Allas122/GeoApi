using System.Net;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public class ValidationTests(PostgresFixture fixture) : ApiIntegrationTest(fixture)
{
    [Fact]
    public async Task MalformedJsonBody_ReturnsSingleBadRequest()
    {
        HttpResponseMessage response = await Client.PostAsync("/resource/batch", ApiJson.Raw("{ broken"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task EmptyBody_ReturnsBadRequestNotServerError()
    {
        HttpResponseMessage response = await Client.PostAsync("/location", ApiJson.Raw(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NumberAsString_IsRejectedByStrictNumberHandling()
    {
        HttpResponseMessage response = await Client.PostAsync(
            "/location",
            ApiJson.Raw("""{"point":{"longitude":"37.6","latitude":55.7}}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingRequiredPoint_ReturnsValidationErrors()
    {
        HttpResponseMessage response = await Client.PostAsync("/location", ApiJson.Raw("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement problem = await ApiJson.ReadAsync(response);
        Assert.True(problem.TryGetProperty("errors", out JsonElement errors));
        Assert.NotEmpty(errors.EnumerateObject());
    }

    [Fact]
    public async Task InvalidLtree_ReturnsValidationErrors()
    {
        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("root.bad-branch", 0, ApiJson.Point(1, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement problem = await ApiJson.ReadAsync(response);
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task OutOfRangeCoordinates_AreRejected()
    {
        HttpResponseMessage response = await ApiJson.PostAsync(
            Client,
            "/location",
            new { point = ApiJson.Point(200, 100) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LimitAboveMaximum_IsRejected()
    {
        HttpResponseMessage response = await Client.GetAsync("/resource?limit=99999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BrokenLqueryPattern_IsRejectedByValidator()
    {
        HttpResponseMessage response = await Client.GetAsync("/resource/search?pattern=***");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClusterWindowOverCellBudget_IsRejected()
    {
        HttpResponseMessage response = await Client.GetAsync(
            "/location/clusters?minLon=-180&minLat=-90&maxLon=180&maxLat=90&gridSize=0.0001");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement problem = await ApiJson.ReadAsync(response);
        Assert.Contains("GridSize", problem.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidRequests_AreNotRejectedAfterEnablingModelValidation()
    {
        HttpResponseMessage create = await ApiJson.PostAsync(
            Client,
            "/location",
            new { point = ApiJson.Point(37.6208, 55.7539) });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        HttpResponseMessage batch = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("root.valid", 0, ApiJson.Point(1, 1)));
        Assert.Equal(HttpStatusCode.OK, batch.StatusCode);

        foreach (string url in new[]
                 {
                     "/resource",
                     "/resource?limit=10",
                     "/resource/subtree?branchPath=root&limit=5",
                     "/resource/ancestors?branchPath=root.valid&limit=5",
                     "/resource/search?pattern=root.*&limit=5",
                     "/location/clusters?minLon=37.4&minLat=55.6&maxLon=37.8&maxLat=55.9&gridSize=0.01",
                     "/location/radius?longitude=37.62&latitude=55.75&radiusMeters=5000&limit=10"
                 })
        {
            HttpResponseMessage response = await Client.GetAsync(url);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"{url} returned {(int)response.StatusCode}");
        }
    }
}
