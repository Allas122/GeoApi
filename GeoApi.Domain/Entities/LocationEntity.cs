using GeoApi.Domain.Geometry;

namespace GeoApi.Domain.Entities;

public class LocationEntity
{
    public int Id { get; set; }
    public required Coordinate Point { get; set; }
}
