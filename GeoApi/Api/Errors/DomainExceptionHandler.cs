using GeoApi.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GeoApi.Api.Errors;

public class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not GeoApiException domainException)
        {
            return false;
        }

        int statusCode = ToStatusCode(domainException);

        if (domainException.InnerException is null)
        {
            logger.LogInformation(
                "Request {Method} {Path} rejected with {StatusCode}: {Message}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                domainException.Message);
        }
        else
        {
            logger.LogWarning(
                domainException,
                "Request {Method} {Path} failed with {StatusCode}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode);
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ErrorStatusCodes.ToTitle(statusCode),
            Detail = domainException.Message
        };

        if (domainException is LocationPointConflictException conflict)
        {
            problemDetails.Extensions["existingLocationId"] = conflict.ExistingLocationId;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = domainException,
            ProblemDetails = problemDetails
        });
    }

    private static int ToStatusCode(GeoApiException exception)
    {
        return exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            InvalidRequestException => StatusCodes.Status400BadRequest,
            OperationTimedOutException => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
