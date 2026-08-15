using GeoApi.Application.Dto;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;
using Riok.Mapperly.Abstractions;

namespace GeoApi.Application.Mappers;

[Mapper]
public static partial class ResourceMapper
{
    public static partial ResourceDto MapToResourceDto(this ResourceEntity resource);

    public static partial IEnumerable<ResourceDto> MapToResourceDtos(this IEnumerable<ResourceEntity> resources);

    public static partial GetResourceByIdParameters MapToParameters(this ResourceByIdQueryDto query);

    public static partial UpdateResourceParameters MapToParameters(this UpdateResourceDto update);

    public static partial GetResourcesByIdsParameters MapToParameters(this ResourcesByIdsQueryDto query);

    [MapperIgnoreSource(nameof(ResourceAncestorsQueryDto.Limit))]
    public static partial GetResourceAncestorsParameters MapToParameters(
        this ResourceAncestorsQueryDto query,
        int limit);

    [MapperIgnoreSource(nameof(ResourcesByLocationIdQueryDto.Limit))]
    public static partial GetResourcesByLocationIdParameters MapToParameters(
        this ResourcesByLocationIdQueryDto query,
        int limit);

    [MapperIgnoreSource(nameof(ResourcePageQueryDto.Limit))]
    public static partial GetResourcesPageParameters MapToParameters(this ResourcePageQueryDto query, int limit);

    [MapperIgnoreSource(nameof(ResourceSubtreeQueryDto.Limit))]
    public static partial GetResourceSubtreeParameters MapToParameters(this ResourceSubtreeQueryDto query, int limit);

    [MapperIgnoreSource(nameof(ResourceBranchPatternQueryDto.Limit))]
    public static partial GetResourcesByBranchPatternParameters MapToParameters(
        this ResourceBranchPatternQueryDto query,
        int limit);

    [MapperIgnoreSource(nameof(ResourceLocationsQueryDto.Limit))]
    public static partial GetResourceLocationsByIdParameters MapToParameters(
        this ResourceLocationsQueryDto query,
        int limit);
}
