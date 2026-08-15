namespace GeoApi.Domain.Exceptions;

public abstract class GeoApiException : Exception
{
    protected GeoApiException(string message) : base(message)
    {
    }

    protected GeoApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class NotFoundException : GeoApiException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class ConflictException : GeoApiException
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class InvalidRequestException : GeoApiException
{
    public InvalidRequestException(string message) : base(message)
    {
    }

    public InvalidRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class OperationTimedOutException : GeoApiException
{
    public OperationTimedOutException(string message) : base(message)
    {
    }

    public OperationTimedOutException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
