using GeoApi.Api;
using GeoApi.Application;
using GeoApi.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddInfrastructure(builder.Configuration)
        .AddApplication()
        .AddApiLayer();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseRouting();
    app.MapControllers();
    app.MapHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "GeoApi terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
