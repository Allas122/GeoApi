using GeoApi.Application.Dto;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.ParameterObjects.Location;
using Riok.Mapperly.Abstractions;
using GridClusterWithResourceIds = GeoApi.Domain.Dto.Location.GridClusterWithResourceIds;

namespace GeoApi.Application.Mappers;

[Mapper]
public static partial class LocationMapper
{
    public static Coordinate MapToCoordinate(this PointDto point)
    {
        return new Coordinate(point.Longitude, point.Latitude);
    }

    public static IEnumerable<Coordinate> MapToCoordinates(this IEnumerable<PointDto> points)
    {
        return points.Select(MapToCoordinate);
    }

    public static PointDto MapToPointDto(this Coordinate coordinate)
    {
        return new PointDto(coordinate.Longitude, coordinate.Latitude);
    }

    public static partial LocationDto MapToLocationDto(this LocationEntity locationEntity);

    public static partial IEnumerable<LocationDto> MapToLocationDtos(this IEnumerable<LocationEntity> locationEntities);

    public static partial GridClusterDto MapToGridClusterDto(this GridClusterWithResourceIds cluster);

    public static partial IEnumerable<GridClusterDto> MapToGridClusterDtos(
        this IEnumerable<GridClusterWithResourceIds> clusters);

    public static GetLocationsInRadiusParameters MapToParameters(this LocationsInRadiusQueryDto query, int limit)
    {
        return new GetLocationsInRadiusParameters
        {
            Center = query.Center.MapToCoordinate(),
            RadiusMeters = query.RadiusMeters,
            LastId = query.LastId,
            Limit = limit
        };
    }

    public static UpdateLocationParameters MapToParameters(this UpdateLocationDto update)
    {
        return new UpdateLocationParameters
        {
            Id = update.Id,
            Point = update.Point.MapToCoordinate()
        };
    }

    public static partial GetWindowedAndClusteredByGridParameters MapToParameters(this LocationClustersQueryDto query);
}
