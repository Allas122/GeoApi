using System.Net;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public class ResourceLifecycleTests(PostgresFixture fixture) : ApiIntegrationTest(fixture)
{
    [Fact]
    public async Task CreateReadUpdateDelete_WalksTheWholeContract()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch(
                "root.lifecycle",
                3600,
                ApiJson.Point(37.6208, 55.7539),
                ApiJson.Point(37.6216, 55.7542)));

        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        JsonElement batch = await ApiJson.ReadAsync(created);
        int resourceId = batch[0].GetProperty("resourceId").GetInt32();
        Assert.Equal(2, batch[0].GetProperty("locationIds").GetArrayLength());

        JsonElement fetched = await ApiJson.ReadAsync(await Client.GetAsync($"/resource/{resourceId}"));
        Assert.Equal("root.lifecycle", fetched.GetProperty("resourceBranch").GetString());
        Assert.Equal(3600, fetched.GetProperty("expiresInSeconds").GetInt64());

        string createdAt = fetched.GetProperty("createdAt").GetString()!;
        string updatedAtBefore = fetched.GetProperty("updatedAt").GetString()!;

        await Task.Delay(20);

        JsonElement updated = await ApiJson.ReadAsync(await ApiJson.PutAsync(
            Client,
            $"/resource/{resourceId}",
            new { resourceBranch = "root.lifecycle.moved", expiresInSeconds = 0 }));

        Assert.Equal("root.lifecycle.moved", updated.GetProperty("resourceBranch").GetString());
        Assert.Equal(createdAt, updated.GetProperty("createdAt").GetString());
        Assert.NotEqual(updatedAtBefore, updated.GetProperty("updatedAt").GetString());

        JsonElement replaced = await ApiJson.ReadAsync(await ApiJson.PutAsync(
            Client,
            $"/resource/{resourceId}/locations",
            new { points = new[] { ApiJson.Point(30.0, 40.0) } }));
        Assert.Equal(1, replaced.GetArrayLength());

        JsonElement locations = await ApiJson.ReadAsync(
            await Client.GetAsync($"/resource/{resourceId}/locations?limit=10"));
        Assert.Equal(1, locations.GetProperty("items").GetArrayLength());

        HttpResponseMessage deleted = await Client.DeleteAsync($"/resource/{resourceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        HttpResponseMessage gone = await Client.GetAsync($"/resource/{resourceId}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task LinkAndUnlinkLocation_ReturnNoContent()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("root.linking", 0, ApiJson.Point(1.0, 1.0)));
        created.EnsureSuccessStatusCode();
        int resourceId = (await ApiJson.ReadAsync(created))[0].GetProperty("resourceId").GetInt32();

        HttpResponseMessage locationCreated = await ApiJson.PostAsync(
            Client,
            "/location",
            new { point = ApiJson.Point(2.0, 2.0) });
        locationCreated.EnsureSuccessStatusCode();
        int locationId = (await ApiJson.ReadAsync(locationCreated)).GetInt32();

        HttpResponseMessage link = await Client.PostAsync($"/resource/{resourceId}/locations/{locationId}", null);
        Assert.Equal(HttpStatusCode.NoContent, link.StatusCode);

        HttpResponseMessage relink = await Client.PostAsync($"/resource/{resourceId}/locations/{locationId}", null);
        Assert.Equal(HttpStatusCode.NoContent, relink.StatusCode);

        HttpResponseMessage unlink = await Client.DeleteAsync($"/resource/{resourceId}/locations/{locationId}");
        Assert.Equal(HttpStatusCode.NoContent, unlink.StatusCode);
    }

    [Fact]
    public async Task ExpiredResource_DisappearsUnlessIncludeExpiredIsRequested()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("root.expiring", 1, ApiJson.Point(5.0, 5.0)));
        created.EnsureSuccessStatusCode();
        int resourceId = (await ApiJson.ReadAsync(created))[0].GetProperty("resourceId").GetInt32();

        await using (System.Data.Common.DbConnection connection = await Fixture.OpenAsync())
        {
            await Dapper.SqlMapper.ExecuteAsync(
                connection,
                "UPDATE public.resources SET created_at = now() - interval '1 hour' WHERE id = @resourceId",
                new { resourceId });
        }

        HttpResponseMessage hidden = await Client.GetAsync($"/resource/{resourceId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        HttpResponseMessage visible = await Client.GetAsync($"/resource/{resourceId}?includeExpired=true");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
    }

    [Fact]
    public async Task BatchCreate_KeepsLocationsWithTheirOwnResources()
    {
        HttpResponseMessage created = await ApiJson.PostAsync(Client, "/resource/batch", new
        {
            resources = new[]
            {
                new { resourceBranch = "batch.a", expiresInSeconds = 0L, points = new[] { ApiJson.Point(1.0, 1.0) } },
                new
                {
                    resourceBranch = "batch.b",
                    expiresInSeconds = 0L,
                    points = new[] { ApiJson.Point(2.0, 2.0), ApiJson.Point(3.0, 3.0) }
                },
                new { resourceBranch = "batch.c", expiresInSeconds = 0L, points = new[] { ApiJson.Point(4.0, 4.0) } }
            }
        });

        created.EnsureSuccessStatusCode();
        JsonElement batch = await ApiJson.ReadAsync(created);

        Assert.Equal(3, batch.GetArrayLength());
        Assert.Equal("batch.a", batch[0].GetProperty("resourceBranch").GetString());
        Assert.Equal(1, batch[0].GetProperty("locationIds").GetArrayLength());
        Assert.Equal("batch.b", batch[1].GetProperty("resourceBranch").GetString());
        Assert.Equal(2, batch[1].GetProperty("locationIds").GetArrayLength());
        Assert.Equal("batch.c", batch[2].GetProperty("resourceBranch").GetString());
        Assert.Equal(1, batch[2].GetProperty("locationIds").GetArrayLength());

        foreach (JsonElement resource in batch.EnumerateArray())
        {
            int id = resource.GetProperty("resourceId").GetInt32();
            JsonElement page = await ApiJson.ReadAsync(await Client.GetAsync($"/resource/{id}/locations?limit=10"));

            Assert.Equal(
                resource.GetProperty("locationIds").GetArrayLength(),
                page.GetProperty("items").GetArrayLength());
        }
    }
}
