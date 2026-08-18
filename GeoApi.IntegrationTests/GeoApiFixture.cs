using System.Data.Common;
using Dapper;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Database;
using GeoApi.Infrastructure.Database.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GeoApi.IntegrationTests;

public class GeoApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:17-3.5")
        .WithDatabase("geoapi")
        .WithUsername("geoapi")
        .WithPassword("localdev")
        .Build();

    private ServiceProvider _rootProvider = null!;
    private GeoApiFactory _factory = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplySchemaAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDatabaseConnectionFactory(BuildConfiguration());
        _rootProvider = services.BuildServiceProvider();

        _factory = new GeoApiFactory(ConnectionString);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _rootProvider.DisposeAsync();
        await _container.DisposeAsync();
    }

    public HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    public AsyncServiceScope CreateScope()
    {
        return _rootProvider.CreateAsyncScope();
    }

    public async Task ResetAsync()
    {
        await using DbConnection connection = await OpenAsync();
        await connection.ExecuteAsync(
            """
            TRUNCATE public.resource_location, public.resources, public.locations RESTART IDENTITY CASCADE
            """);
    }

    public async Task<DbConnection> OpenAsync()
    {
        var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:ConnectionString"] = ConnectionString,
                ["DatabaseSettings:CommandTimeout"] = "30",
                ["DatabaseSettings:Logging"] = "false"
            })
            .Build();
    }

    private async Task ApplySchemaAsync()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Schema");
        string[] scripts = Directory
            .GetFiles(root, "Up.sql", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(scripts);

        await using DbConnection connection = await OpenAsync();
        foreach (string script in scripts)
        {
            await connection.ExecuteAsync(await File.ReadAllTextAsync(script));
        }
    }
}

[CollectionDefinition(Name)]
public class GeoApiCollection : ICollectionFixture<GeoApiFixture>
{
    public const string Name = "geoapi";
}

public abstract class IntegrationTest : IAsyncLifetime
{
    protected IntegrationTest(GeoApiFixture fixture)
    {
        Fixture = fixture;
    }

    protected GeoApiFixture Fixture { get; }

    private AsyncServiceScope _scope;

    protected IResourceRepository Resources => _scope.ServiceProvider.GetRequiredService<IResourceRepository>();
    protected ILocationRepository Locations => _scope.ServiceProvider.GetRequiredService<ILocationRepository>();
    protected IUnitOfWork UnitOfWork => _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    public async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
        _scope = Fixture.CreateScope();
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }

    protected AsyncServiceScope NewScope()
    {
        return Fixture.CreateScope();
    }
}

public abstract class ApiIntegrationTest : IAsyncLifetime
{
    protected ApiIntegrationTest(GeoApiFixture fixture)
    {
        Fixture = fixture;
    }

    protected GeoApiFixture Fixture { get; }

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
        Client = Fixture.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
