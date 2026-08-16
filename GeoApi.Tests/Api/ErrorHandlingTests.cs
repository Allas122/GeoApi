using System.Text.Json;
using GeoApi.Api.Errors;
using GeoApi.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace GeoApi.Tests.Api;

public class DomainExceptionHandlerTests
{
    private static (DomainExceptionHandler Handler, DefaultHttpContext Context, MemoryStream Body) CreateSut()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();

        ServiceProvider provider = services.BuildServiceProvider();

        var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = body;
        context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(body));
        context.Request.Method = "GET";
        context.Request.Path = "/resource/1";

        var handler = new DomainExceptionHandler(
            provider.GetRequiredService<IProblemDetailsService>(),
            NullLogger<DomainExceptionHandler>.Instance);

        return (handler, context, body);
    }

    private static JsonElement ReadBody(MemoryStream body)
    {
        body.Position = 0;
        return JsonDocument.Parse(body).RootElement;
    }

    [Fact]
    public async Task Handles_NotFoundAs404()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, MemoryStream body) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new ResourceNotFoundException(7),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        JsonElement problem = ReadBody(body);
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Contains("7", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Handles_LocationNotFoundAs404()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, _) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new LocationNotFoundException(11),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handles_LinkNotFoundAs404()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, _) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new ResourceLocationLinkNotFoundException(7, 11),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handles_PointConflictAs409WithExistingLocationId()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, MemoryStream body) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new LocationPointConflictException(99),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        JsonElement problem = ReadBody(body);
        Assert.Equal(99, problem.GetProperty("existingLocationId").GetInt32());
    }

    [Fact]
    public async Task Handles_TimeoutAs504()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, _) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new OperationTimedOutException("The database did not respond in time."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handles_UnmappedDomainExceptionAs500()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, MemoryStream body) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new UnmappedDomainException("something went sideways"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        JsonElement problem = ReadBody(body);
        Assert.Equal("Server Error", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Handles_ExceptionWithInnerCauseAs400()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, MemoryStream body) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidRequestException("The request contains a value the database rejected.", new Exception("42601")),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        JsonElement problem = ReadBody(body);
        Assert.Equal("Bad Request", problem.GetProperty("title").GetString());
        Assert.DoesNotContain("42601", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Ignores_NonDomainExceptions()
    {
        (DomainExceptionHandler handler, DefaultHttpContext context, _) = CreateSut();

        bool handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }

    private sealed class UnmappedDomainException(string message) : GeoApiException(message);
}
