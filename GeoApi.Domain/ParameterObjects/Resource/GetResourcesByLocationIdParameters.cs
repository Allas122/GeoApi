
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourcesByLocationIdParameters : IPaginatedById, IExpiryFiltered
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public required int LocationId { get; set; }

    public bool IncludeExpired { get; set; }
}
