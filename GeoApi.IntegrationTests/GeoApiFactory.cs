using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GeoApi.IntegrationTests;

public class GeoApiFactory(string connectionString, string environment = "Production") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("DatabaseSettings:ConnectionString", connectionString);
        builder.UseSetting("DatabaseSettings:CommandTimeout", "30");
        builder.UseSetting("DatabaseSettings:Logging", "false");
        builder.UseSetting("Serilog:MinimumLevel:Override:Microsoft.AspNetCore.HttpsPolicy", "Fatal");
    }
}
