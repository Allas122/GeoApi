namespace GeoApi.Domain.Exceptions;

public class LocationNotFoundException : NotFoundException
{
    public LocationNotFoundException(int locationId)
        : base($"Location {locationId} was not found.")
    {
        LocationId = locationId;
    }

    public int LocationId { get; }
}

public class LocationPointConflictException : ConflictException
{
    public LocationPointConflictException(int existingLocationId)
        : base("Another location already occupies these coordinates.")
    {
        ExistingLocationId = existingLocationId;
    }

    public int ExistingLocationId { get; }
}
