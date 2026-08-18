using System.Net;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(GeoApiCollection.Name)]
public class OpenApiDocumentTests(GeoApiFixture fixture) : IAsyncLifetime
{
    private GeoApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new GeoApiFactory(fixture.ConnectionString, "Development");
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Document_IsServedWithProjectMetadata()
    {
        HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        JsonElement document = await ApiJson.ReadAsync(response);
        JsonElement info = document.GetProperty("info");

        Assert.Equal("GeoApi", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        Assert.False(string.IsNullOrWhiteSpace(info.GetProperty("description").GetString()));
    }

    [Fact]
    public async Task Document_DescribesTheBulkEndpoints()
    {
        JsonElement document = await ApiJson.ReadAsync(await _client.GetAsync("/openapi/v1.json"));
        JsonElement paths = document.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/location/batch", out _));
        Assert.True(paths.TryGetProperty("/resource/{id}/locations", out _));
    }

    [Fact]
    public async Task Document_DescribesIntegersWithoutStringUnion()
    {
        JsonElement document = await ApiJson.ReadAsync(await _client.GetAsync("/openapi/v1.json"));
        JsonElement expiresInSeconds = document
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("ResourceResponse")
            .GetProperty("properties")
            .GetProperty("expiresInSeconds");

        Assert.Equal("integer", expiresInSeconds.GetProperty("type").GetString());
        Assert.Equal("int64", expiresInSeconds.GetProperty("format").GetString());
        Assert.False(expiresInSeconds.TryGetProperty("pattern", out _));
    }

    [Fact]
    public async Task ApiReference_IsAvailableInDevelopment()
    {
        HttpResponseMessage response = await _client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
