using GeoApi.Api.Dto;
using GeoApi.Api.Messages;
using GeoApi.Application.Dto;
using Riok.Mapperly.Abstractions;

namespace GeoApi.Api.Mappers;

[Mapper]
public static partial class ResourceMapper
{
    private static TimeSpan ToTimeSpan(long seconds)
    {
        return TimeSpan.FromSeconds(seconds);
    }

    private static long ToSeconds(TimeSpan value)
    {
        return (long)value.TotalSeconds;
    }

    [MapProperty(
        nameof(CreateResourceWithLocationsMessage.ExpiresInSeconds),
        nameof(CreateResourceWithLocationsDto.ExpiresIn))]
    public static partial CreateResourceWithLocationsDto MapToCreateResourceDto(
        this CreateResourceWithLocationsMessage message);

    public static partial IReadOnlyList<CreateResourceWithLocationsDto> MapToCreateResourceDtos(
        this IReadOnlyList<CreateResourceWithLocationsMessage> messages);

    [MapProperty(nameof(CreatedResourceDto.ExpiresIn), nameof(CreatedResourceResponse.ExpiresInSeconds))]
    public static partial CreatedResourceResponse MapToResponse(this CreatedResourceDto resource);

    public static partial IReadOnlyList<CreatedResourceResponse> MapToResponses(
        this IReadOnlyList<CreatedResourceDto> resources);

    [MapProperty(nameof(ResourceDto.ExpiresIn), nameof(ResourceResponse.ExpiresInSeconds))]
    public static partial ResourceResponse MapToResponse(this ResourceDto resource);

    public static partial IReadOnlyList<ResourceResponse> MapToResponses(this IReadOnlyList<ResourceDto> resources);

    public static ResourcesByIdsQueryDto MapToQueryDto(this GetResourcesByIdsQuery query)
    {
        return new ResourcesByIdsQueryDto(query.Ids, query.IncludeExpired);
    }

    public static BulkUnlinkResourceLocationsDto MapToUnlinkDto(
        this UnlinkResourceLocationsQuery query,
        int resourceId)
    {
        return new BulkUnlinkResourceLocationsDto(resourceId, query.LocationIds);
    }

    public static UpdateResourceDto MapToUpdateDto(this UpdateResourceMessage message, int id)
    {
        return new UpdateResourceDto(id, message.ResourceBranch, ToTimeSpan(message.ExpiresInSeconds));
    }

    public static ReplaceResourceLocationsDto MapToReplacementDto(
        this ReplaceResourceLocationsMessage message,
        int resourceId)
    {
        return new ReplaceResourceLocationsDto(resourceId, message.Points.MapToPointDtos());
    }

    public static partial ResourcePageQueryDto MapToQueryDto(this GetResourcesPageQuery query);

    public static partial ResourceSubtreeQueryDto MapToQueryDto(this GetResourceSubtreeQuery query);

    public static partial ResourceBranchPatternQueryDto MapToQueryDto(this GetResourcesByBranchPatternQuery query);

    public static partial ResourceAncestorsQueryDto MapToQueryDto(this GetResourceAncestorsQuery query);

    public static ResourceLocationsQueryDto MapToQueryDto(this GetResourceLocationsQuery query, int resourceId)
    {
        return new ResourceLocationsQueryDto(resourceId, query.LastId, query.Limit, query.IncludeExpired);
    }

    public static ResourceByIdQueryDto MapToQueryDto(this GetResourceByIdQuery query, int id)
    {
        return new ResourceByIdQueryDto(id, query.IncludeExpired);
    }

    public static ResourcesByLocationIdQueryDto MapToQueryDto(this GetResourcesByLocationIdQuery query, int locationId)
    {
        return new ResourcesByLocationIdQueryDto(locationId, query.LastId, query.Limit, query.IncludeExpired);
    }
}
