
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourceSubtreeParameters : IPaginatedById, IExpiryFiltered
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public required string BranchPath { get; set; }
    public int? MaxDepth { get; set; }
    public bool IncludeSelf { get; set; } = true;

    public bool IncludeExpired { get; set; }
}
