using System.Data.Common;
using System.Net;
using System.Net.Http.Json;

namespace GeoApi.IntegrationTests.Api;

[Collection(GeoApiCollection.Name)]
public class CancellationContractTests(GeoApiFixture fixture) : ApiIntegrationTest(fixture)
{
    private const string DeadDatabase =
        "Host=127.0.0.1;Port=1;Username=geoapi;Password=localdev;Database=geoapi;Timeout=1;Command Timeout=1";

    [Fact]
    public async Task ClientAbort_AbortsDatabaseWorkAndWritesNothing()
    {
        await using DbConnection blocker = await Fixture.OpenAsync();
        await using DbTransaction blockingTransaction = await blocker.BeginTransactionAsync();
        await CancellationProbe.LockLocationsAsync(blocker, blockingTransaction);

        using var cts = new CancellationTokenSource();
        Task<HttpResponseMessage> pending = Client.PostAsJsonAsync(
            "/location",
            new { point = ApiJson.Point(45.0, 45.0) },
            ApiJson.Options,
            cts.Token);

        await CancellationProbe.WaitForBlockedInsertAsync(Fixture, expected: true);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await CancellationProbe.WaitForBlockedInsertAsync(Fixture, expected: false);

        await blockingTransaction.RollbackAsync();
        Assert.Equal(0, await CancellationProbe.CountLocationsAsync(Fixture));
    }

    [Fact]
    public async Task Health_WithUnreachableDatabase_ReportsUnhealthy()
    {
        await using var factory = new GeoApiFactory(DeadDatabase);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_WithUnreachableDatabase_ReturnsProblemDetailsWithoutDriverDetails()
    {
        await using var factory = new GeoApiFactory(DeadDatabase);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await ApiJson.PostAsync(
            client,
            "/location",
            new { point = ApiJson.Point(46.0, 46.0) });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.OrdinalIgnoreCase);
    }
}