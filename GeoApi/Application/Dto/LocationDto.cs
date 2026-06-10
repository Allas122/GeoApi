using NetTopologySuite.Geometries;

namespace GeoApi.Application.Dto;

public class LocationDto
{
    public int Id { get; set; }
    public Point Point { get; set; }
}