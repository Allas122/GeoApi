using GeoApi.Domain.Dto.Location;
using GeoApi.Domain.Entities;
using GeoApi.Infrastructure.Database.DataTypes;
using NetTopologySuite.Geometries;

using Coordinate = GeoApi.Domain.Geometry.Coordinate;

namespace GeoApi.Infrastructure.Mappers;

public static class LocationMapper
{
    public const int Srid = 4326;

    public static Point MapToPoint(this Coordinate coordinate)
    {
        return new Point(coordinate.Longitude, coordinate.Latitude) { SRID = Srid };
    }

    public static Point[] MapToPoints(this IEnumerable<Coordinate> coordinates)
    {
        return coordinates.Select(MapToPoint).ToArray();
    }

    public static LocationEntity MapToLocation(this LocationRow row)
    {
        return new LocationEntity
        {
            Id = row.Id,
            Point = new Coordinate(row.Longitude, row.Latitude)
        };
    }

    public static IEnumerable<LocationEntity> MapToLocations(this IEnumerable<LocationRow> rows)
    {
        return rows.Select(MapToLocation);
    }

    public static GridClusterWithResourceIds MapToGridCluster(this GridClusterRow row)
    {
        return new GridClusterWithResourceIds
        {
            Center = new Coordinate(row.CenterLongitude, row.CenterLatitude),
            Count = row.Count,
            ResourceCount = row.ResourceCount,
            ResourceIds = row.ResourceIds,
        };
    }
}
