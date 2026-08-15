namespace GeoApi.Domain.Exceptions;

public class ResourceNotFoundException : NotFoundException
{
    public ResourceNotFoundException(int resourceId)
        : base($"Resource {resourceId} was not found.")
    {
        ResourceId = resourceId;
    }

    public int ResourceId { get; }
}

public class ResourceLocationLinkNotFoundException : NotFoundException
{
    public ResourceLocationLinkNotFoundException(int resourceId, int locationId)
        : base($"Resource {resourceId} is not linked to location {locationId}.")
    {
        ResourceId = resourceId;
        LocationId = locationId;
    }

    public int ResourceId { get; }
    public int LocationId { get; }
}
