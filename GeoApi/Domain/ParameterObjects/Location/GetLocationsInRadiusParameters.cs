using GeoApi.Domain.Entities;
using NetTopologySuite.Geometries;

namespace GeoApi.Domain.ParameterObjects.Location;

public record GetLocationsInRadiusParameters : IPaginatedById
{
    public int LastId{ get; set; }
    public int Limit{ get; set; }
    
    public required Point Location { get; set; }
    public double RadiusMeters{ get; set; }

}