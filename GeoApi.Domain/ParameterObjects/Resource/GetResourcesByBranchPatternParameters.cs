
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourcesByBranchPatternParameters : IPaginatedById, IExpiryFiltered
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public required string Pattern { get; set; }

    public bool IncludeExpired { get; set; }
}
