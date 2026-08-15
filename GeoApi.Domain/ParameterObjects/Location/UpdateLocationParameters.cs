using GeoApi.Domain.Geometry;

namespace GeoApi.Domain.ParameterObjects.Location;

public record UpdateLocationParameters
{
    public required int Id { get; set; }
    public required Coordinate Point { get; set; }
}
