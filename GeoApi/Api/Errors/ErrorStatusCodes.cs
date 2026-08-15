namespace GeoApi.Api.Errors;

public static class ErrorStatusCodes
{
    public static string ToTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status504GatewayTimeout => "Gateway Timeout",
            _ => "Server Error"
        };
    }
}
