using Microsoft.OpenApi;
using NetTopologySuite.Geometries;


namespace GeoApi.Api.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services)
    {
             services.AddOpenApi(options =>
             {
                 options.AddDocumentTransformer((document, context, cancellationToken) =>
                 {
                     document.Info.Title = "GeoApi";
                     document.Info.Version = "v1";
                     document.Info.Description = "API для работы с PostGIS";
                     return Task.CompletedTask;
                 });
             });
        
        return services;
    }
}