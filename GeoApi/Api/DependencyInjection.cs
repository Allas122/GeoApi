using System.Globalization;
using FluentValidation;
using GeoApi.Api.Configuration;
using GeoApi.Api.Errors;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace GeoApi.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services)
    {
        services.AddLoggingConfiguration();
        services.AddLocalizationConfiguration();
        services.AddOpenApiConfiguration();
        services.AddValidationConfiguration();
        services.AddErrorHandling();
        return services;
    }

    private static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<DomainExceptionHandler>();
        return services;
    }

    private static IServiceCollection AddValidationConfiguration(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("en-US");

        services.AddValidatorsFromAssemblyContaining<Program>();

        services.AddFluentValidationAutoValidation(configuration =>
        {
            configuration.EnableFormBindingSourceAutomaticValidation = false;
        });

        return services;
    }
}
