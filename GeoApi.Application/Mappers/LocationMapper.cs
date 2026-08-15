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

    public static partial IEnumerable<Coordinate> MapToCoordinates(this IEnumerable<PointDto> points);

    public static partial PointDto MapToPointDto(this Coordinate coordinate);

    public static partial LocationDto MapToLocationDto(this LocationEntity locationEntity);

    public static partial IEnumerable<LocationDto> MapToLocationDtos(this IEnumerable<LocationEntity> locationEntities);

    public static partial GridClusterDto MapToGridClusterDto(this GridClusterWithResourceIds cluster);

    public static partial IEnumerable<GridClusterDto> MapToGridClusterDtos(
        this IEnumerable<GridClusterWithResourceIds> clusters);

    [MapperIgnoreSource(nameof(LocationsInRadiusQueryDto.Limit))]
    public static partial GetLocationsInRadiusParameters MapToParameters(
        this LocationsInRadiusQueryDto query,
        int limit);

    public static partial UpdateLocationParameters MapToParameters(this UpdateLocationDto update);

    public static partial GetWindowedAndClusteredByGridParameters MapToParameters(this LocationClustersQueryDto query);
}
