
namespace GeoApi.Domain.ParameterObjects.Resource;

public record GetResourceByIdParameters : IExpiryFiltered
{
    public required int Id { get; set; }

    public bool IncludeExpired { get; set; }
}
