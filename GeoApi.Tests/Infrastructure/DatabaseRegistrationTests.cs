using System.Data.Common;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Database;
using GeoApi.Infrastructure.Database.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Tests.Infrastructure;

public class DatabaseRegistrationTests
{
    private static ServiceProvider BuildProvider(bool logging)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:ConnectionString"] =
                    "Host=localhost;Port=5432;Username=geoapi;Password=localdev;Database=geoapi",
                ["DatabaseSettings:CommandTimeout"] = "30",
                ["DatabaseSettings:Logging"] = logging ? "true" : "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDatabaseConnectionFactory(configuration);

        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDatabaseConnectionFactory_BuildsDataSourceForEitherLoggingSetting(bool logging)
    {
        using ServiceProvider provider = BuildProvider(logging);

        var dataSource = provider.GetRequiredService<DbDataSource>();

        Assert.Contains("Database=geoapi", dataSource.ConnectionString);
    }

    [Fact]
    public async Task AddDatabaseConnectionFactory_SharesOneSessionAcrossRepositoriesInAScope()
    {
        await using ServiceProvider provider = BuildProvider(false);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        var session = scope.ServiceProvider.GetRequiredService<IDbSession>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Assert.Same(session, unitOfWork);
    }

    [Fact]
    public async Task AddDatabaseConnectionFactory_GivesEachScopeItsOwnSession()
    {
        await using ServiceProvider provider = BuildProvider(false);
        await using AsyncServiceScope first = provider.CreateAsyncScope();
        await using AsyncServiceScope second = provider.CreateAsyncScope();

        Assert.NotSame(
            first.ServiceProvider.GetRequiredService<IDbSession>(),
            second.ServiceProvider.GetRequiredService<IDbSession>());
    }
}
