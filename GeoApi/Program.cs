using GeoApi.Api;
using GeoApi.Application;
using GeoApi.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApiLayer();

var app = builder.Build();

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

public partial class Program;
