using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using dotenv.net;

DotEnv.Load();

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();
    
var connectionString = configuration["connection"] ?? configuration["DB_CONNECTION"] ?? configuration.GetConnectionString("Default");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Could not find connection string");
}

var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
Console.WriteLine("Доступные ресурсы:");
foreach (var resource in resources)
{
    Console.WriteLine("- " + resource);
}

var serviceCollection = new ServiceCollection()
    .AddFluentMigratorCore()
    .ConfigureRunner(r=> 
        r.AddPostgres()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations()
    )
    .AddLogging(lb => lb.AddFluentMigratorConsole())
    .BuildServiceProvider();

using var scope = serviceCollection.CreateScope();
var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
if (args.Contains("--rollback"))
{
    runner.MigrateDown(1);
}else
{
    runner.MigrateUp();
}
