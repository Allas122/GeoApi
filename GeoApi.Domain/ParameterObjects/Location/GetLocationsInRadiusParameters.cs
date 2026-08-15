using GeoApi.Domain.Geometry;

namespace GeoApi.Domain.ParameterObjects.Location;

public record GetLocationsInRadiusParameters : IPaginatedById
{
    public int LastId { get; set; }
    public int Limit { get; set; }

    public required Coordinate Center { get; set; }
    public double RadiusMeters { get; set; }
}
