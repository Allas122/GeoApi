using System.Text.Json;
using System.Text.Json.Serialization;
using NetTopologySuite.IO.Converters;

namespace GeoApi.Api.Configuration;

public static class LocalizationConfiguration
{
    public static IServiceCollection AddLocalizationConfiguration(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(
                options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory());
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict; 
                });
        services.Configure<RouteOptions>(options => 
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });
        return services;
    }
}