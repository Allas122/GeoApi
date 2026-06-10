using NetTopologySuite.Geometries;

namespace GeoApi.Domain.Entities;

public class LocationEntity
{
    public int Id { get; set; }
    public Point Point { get; set; }
}