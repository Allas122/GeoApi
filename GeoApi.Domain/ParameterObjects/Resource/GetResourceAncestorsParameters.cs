
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourceAncestorsParameters : IPaginatedById, IExpiryFiltered
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public required string BranchPath { get; set; }

    public bool IncludeExpired { get; set; }
}
