using System.Net;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(GeoApiCollection.Name)]
public class PaginationContractTests(GeoApiFixture fixture) : ApiIntegrationTest(fixture)
{
    [Fact]
    public async Task ResourcePage_WalksEveryRowExactlyOnceByFollowingNextLastId()
    {
        string[] branches = Enumerable.Range(0, 12).Select(i => $"page.n{i}").ToArray();
        await SeedAsync(branches);

        string[] seen = await DrainAsync("/resource?limit=5", "resourceBranch");

        Assert.Equal(branches.Order(), seen.Order());
        Assert.Equal(seen.Length, seen.Distinct().Count());
    }

    [Fact]
    public async Task SubtreePage_WalksEveryRowExactlyOnceByFollowingNextLastId()
    {
        string[] branches = Enumerable.Range(0, 9).Select(i => $"tree.n{i}").ToArray();
        await SeedAsync(branches);

        string[] seen = await DrainAsync("/resource/subtree?branchPath=tree&limit=4", "resourceBranch");

        Assert.Equal(branches.Order(), seen.Order());
    }

    [Fact]
    public async Task ResourceLocationsPage_WalksEveryLinkExactlyOnce()
    {
        object[] points = Enumerable.Range(0, 7).Select(i => ApiJson.Point(70.0 + i, 10.0 + i)).ToArray();

        HttpResponseMessage created = await ApiJson.PostAsync(
            Client,
            "/resource/batch",
            ApiJson.ResourceBatch("paged.locations", 0, points));
        created.EnsureSuccessStatusCode();

        JsonElement batch = await ApiJson.ReadAsync(created);
        int resourceId = batch[0].GetProperty("resourceId").GetInt32();
        int[] expected = batch[0].GetProperty("locationIds")
            .EnumerateArray()
            .Select(id => id.GetInt32())
            .Order()
            .ToArray();

        string[] seen = await DrainAsync($"/resource/{resourceId}/locations?limit=3", "id");

        Assert.Equal(expected, seen.Select(int.Parse).Order().ToArray());
    }

    [Fact]
    public async Task LastPage_ReportsHasMoreFalse()
    {
        await SeedAsync(["tail.a", "tail.b"]);

        JsonElement page = await ReadPageAsync("/resource?limit=5");

        Assert.False(page.GetProperty("hasMore").GetBoolean());
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task EmptyPage_OmitsTheCursorEntirely()
    {
        JsonElement page = await ReadPageAsync("/resource?limit=5");

        Assert.Empty(page.GetProperty("items").EnumerateArray());
        Assert.False(page.GetProperty("hasMore").GetBoolean());
        Assert.False(page.TryGetProperty("nextLastId", out _));
    }

    [Fact]
    public async Task NonEmptyPage_CarriesTheCursor()
    {
        await SeedAsync(["cursor.a"]);

        JsonElement page = await ReadPageAsync("/resource?limit=5");

        Assert.True(page.TryGetProperty("nextLastId", out JsonElement nextLastId));
        Assert.Equal(JsonValueKind.Number, nextLastId.ValueKind);
    }

    [Fact]
    public async Task LastIdBeyondTheEnd_ReturnsAnEmptyPage()
    {
        await SeedAsync(["beyond.a"]);

        JsonElement page = await ReadPageAsync("/resource?limit=5&lastId=999999");

        Assert.Empty(page.GetProperty("items").EnumerateArray());
        Assert.False(page.GetProperty("hasMore").GetBoolean());
    }

    [Fact]
    public async Task LimitAboveTheCap_IsRejectedRatherThanSilentlyClamped()
    {
        HttpResponseMessage response = await Client.GetAsync("/resource?limit=501");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string[]> DrainAsync(string url, string property)
    {
        var seen = new List<string>();
        string separator = url.Contains('?') ? "&" : "?";
        int? lastId = null;
        int guard = 0;

        while (guard++ < 50)
        {
            string page = lastId is null ? url : $"{url}{separator}lastId={lastId}";
            JsonElement body = await ReadPageAsync(page);

            foreach (JsonElement item in body.GetProperty("items").EnumerateArray())
            {
                JsonElement value = item.GetProperty(property);
                seen.Add(value.ValueKind == JsonValueKind.Number
                    ? value.GetInt32().ToString()
                    : value.GetString()!);
            }

            if (!body.GetProperty("hasMore").GetBoolean())
            {
                return seen.ToArray();
            }

            lastId = body.GetProperty("nextLastId").GetInt32();
        }

        Assert.Fail("The cursor never reached the end of the collection.");
        return [];
    }

    private async Task<JsonElement> ReadPageAsync(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(url);
        Assert.True(response.IsSuccessStatusCode, $"{url} returned {(int)response.StatusCode}");
        return await ApiJson.ReadAsync(response);
    }

    private async Task SeedAsync(string[] branches)
    {
        HttpResponseMessage response = await ApiJson.PostAsync(Client, "/resource/batch", new
        {
            resources = branches
                .Select((branch, index) => new
                {
                    resourceBranch = branch,
                    expiresInSeconds = 0L,
                    points = new[] { ApiJson.Point(1.0 + index, 1.0 + index) }
                })
                .ToArray()
        });

        response.EnsureSuccessStatusCode();
    }
}
