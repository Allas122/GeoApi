
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourceLocationsByIdParameters : IPaginatedById, IExpiryFiltered
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public required int ResourceId { get; set; }

    public bool IncludeExpired { get; set; }
}
