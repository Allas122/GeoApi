using GeoApi.Api.Dto;
using NetTopologySuite.Geometries;
using Riok.Mapperly.Abstractions;

namespace GeoApi.Api.Mappers;

[Mapper]
public static partial class LocationMapper
{
    public static Point MapToPoint(this PointDto pointDto)
    {
        return new Point(pointDto.Longitude, pointDto.Latitude) { SRID = 4326};
    }
    [MapProperty(nameof(Point.X), nameof(PointDto.Longitude))]
    [MapProperty(nameof(Point.Y), nameof(PointDto.Latitude))]
    public static partial PointDto MapToPointDto(this Point point);
}