using GeoApi.Domain.Exceptions;
using Npgsql;

namespace GeoApi.Infrastructure.Database.Errors;

public static class PostgresExceptionTranslator
{
    public static GeoApiException? Translate(string? sqlState)
    {
        return Create(sqlState, null);
    }

    public static GeoApiException? Translate(PostgresException exception)
    {
        return Create(exception.SqlState, exception);
    }

    private static GeoApiException? Create(string? sqlState, Exception? inner)
    {
        string? message = ToMessage(sqlState);
        if (message is null)
        {
            return null;
        }

        return sqlState switch
        {
            PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation =>
                inner is null ? new ConflictException(message) : new ConflictException(message, inner),
            PostgresErrorCodes.QueryCanceled =>
                inner is null
                    ? new OperationTimedOutException(message)
                    : new OperationTimedOutException(message, inner),
            _ =>
                inner is null ? new InvalidRequestException(message) : new InvalidRequestException(message, inner)
        };
    }

    private static string? ToMessage(string? sqlState)
    {
        return sqlState switch
        {
            PostgresErrorCodes.UniqueViolation => "The request conflicts with the current state of the data.",
            PostgresErrorCodes.ForeignKeyViolation => "The request references data that no longer exists.",
            PostgresErrorCodes.SyntaxError => "The request contains a value the database rejected.",
            PostgresErrorCodes.ProgramLimitExceeded => "The request contains a value that exceeds a database limit.",
            PostgresErrorCodes.NameTooLong => "The request contains a value that exceeds a database limit.",
            PostgresErrorCodes.InvalidTextRepresentation => "The request contains a value the database rejected.",
            PostgresErrorCodes.CheckViolation => "The request violates a data constraint.",
            PostgresErrorCodes.QueryCanceled => "The database did not respond in time.",
            _ => null
        };
    }
}
