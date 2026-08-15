using GeoApi.Domain.Exceptions;
using GeoApi.Infrastructure.Database.Errors;
using Npgsql;

namespace GeoApi.Tests.Infrastructure;

public class PostgresExceptionTranslatorTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    public void Translate_MapsConstraintViolationsToConflict(string sqlState)
    {
        Assert.IsAssignableFrom<ConflictException>(PostgresExceptionTranslator.Translate(sqlState));
    }

    [Theory]
    [InlineData(PostgresErrorCodes.SyntaxError)]
    [InlineData(PostgresErrorCodes.ProgramLimitExceeded)]
    [InlineData(PostgresErrorCodes.NameTooLong)]
    [InlineData(PostgresErrorCodes.InvalidTextRepresentation)]
    [InlineData(PostgresErrorCodes.CheckViolation)]
    public void Translate_MapsBadInputToInvalidRequest(string sqlState)
    {
        Assert.IsAssignableFrom<InvalidRequestException>(PostgresExceptionTranslator.Translate(sqlState));
    }

    [Fact]
    public void Translate_MapsQueryCanceledToTimeout()
    {
        Assert.IsAssignableFrom<OperationTimedOutException>(
            PostgresExceptionTranslator.Translate(PostgresErrorCodes.QueryCanceled));
    }

    [Theory]
    [InlineData("08006")]
    [InlineData("XX000")]
    [InlineData("")]
    [InlineData(null)]
    public void Translate_LeavesUnknownSqlStatesAlone(string? sqlState)
    {
        Assert.Null(PostgresExceptionTranslator.Translate(sqlState));
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.SyntaxError)]
    [InlineData(PostgresErrorCodes.QueryCanceled)]
    public void Translate_KeepsOriginalExceptionForDiagnostics(string sqlState)
    {
        var original = new PostgresException("boom", "ERROR", "ERROR", sqlState);

        GeoApiException? translated = PostgresExceptionTranslator.Translate(original);

        Assert.NotNull(translated);
        Assert.Same(original, translated.InnerException);
    }

    [Fact]
    public void Translate_WithoutOriginal_HasNoInnerException()
    {
        GeoApiException? translated = PostgresExceptionTranslator.Translate(PostgresErrorCodes.UniqueViolation);

        Assert.NotNull(translated);
        Assert.Null(translated.InnerException);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.UniqueViolation)]
    [InlineData(PostgresErrorCodes.SyntaxError)]
    [InlineData(PostgresErrorCodes.QueryCanceled)]
    public void Translate_DoesNotLeakDatabaseWording(string sqlState)
    {
        GeoApiException? translated = PostgresExceptionTranslator.Translate(sqlState);

        Assert.NotNull(translated);
        Assert.DoesNotContain("SQL", translated.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres", translated.Message, StringComparison.OrdinalIgnoreCase);
    }
}
