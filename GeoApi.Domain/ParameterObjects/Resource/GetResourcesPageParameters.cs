
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourcesPageParameters : IPaginatedById, IExpiryFiltered
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public bool IncludeExpired { get; set; }
}
