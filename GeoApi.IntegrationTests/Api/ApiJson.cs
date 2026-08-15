using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GeoApi.IntegrationTests.Api;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static StringContent Raw(string json)
    {
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object body)
    {
        return client.PostAsJsonAsync(url, body, Options);
    }

    public static Task<HttpResponseMessage> PutAsync(HttpClient client, string url, object body)
    {
        return client.PutAsJsonAsync(url, body, Options);
    }

    public static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        await using Stream stream = await response.Content.ReadAsStreamAsync();
        JsonDocument document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }

    public static object Point(double longitude, double latitude)
    {
        return new { longitude, latitude };
    }

    public static object ResourceBatch(string branch, long expiresInSeconds, params object[] points)
    {
        return new
        {
            resources = new[]
            {
                new { resourceBranch = branch, expiresInSeconds, points }
            }
        };
    }
}
