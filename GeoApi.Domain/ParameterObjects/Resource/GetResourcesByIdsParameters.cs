
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourcesByIdsParameters : IExpiryFiltered
{
    public required IReadOnlyList<int> Ids { get; set; }

    public bool IncludeExpired { get; set; }
}
