using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GeoApi.IntegrationTests;

public class GeoApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("DatabaseSettings:ConnectionString", connectionString);
        builder.UseSetting("DatabaseSettings:CommandTimeout", "30");
        builder.UseSetting("DatabaseSettings:Logging", "false");
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore.HttpsPolicy", "None");
    }
}
