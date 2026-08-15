using GeoApi.Api.Dto;
using GeoApi.Api.Messages;
using GeoApi.Application.Dto;
using Riok.Mapperly.Abstractions;
using ApplicationGridClusterDto = GeoApi.Application.Dto.GridClusterDto;
using ApplicationLocationDto = GeoApi.Application.Dto.LocationDto;
using ApplicationPointDto = GeoApi.Application.Dto.PointDto;
using PointDto = GeoApi.Api.Dto.PointDto;

namespace GeoApi.Api.Mappers;

[Mapper]
public static partial class LocationMapper
{
    public static partial ApplicationPointDto MapToPointDto(this PointDto point);

    public static partial IReadOnlyList<ApplicationPointDto> MapToPointDtos(this IReadOnlyList<PointDto> points);

    public static partial PointDto MapToResponsePoint(this ApplicationPointDto point);

    public static partial LocationResponse MapToResponse(this ApplicationLocationDto location);

    public static partial UpdateLocationDto MapToUpdateDto(this UpdateLocationMessage message, int id);

    public static partial GridClusterResponse MapToResponse(this ApplicationGridClusterDto cluster);

    public static partial IReadOnlyList<GridClusterResponse> MapToResponses(
        this IReadOnlyList<ApplicationGridClusterDto> clusters);

    public static LocationsInRadiusQueryDto MapToQueryDto(this GetLocationsInRadiusQuery query)
    {
        return new LocationsInRadiusQueryDto(
            new ApplicationPointDto(query.Longitude, query.Latitude),
            query.RadiusMeters,
            query.LastId,
            query.Limit);
    }

    public static partial LocationClustersQueryDto MapToQueryDto(this GetLocationClustersQuery query);
}
