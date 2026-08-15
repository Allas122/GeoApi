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

    public static GetResourceByIdParameters MapToParameters(this ResourceByIdQueryDto query)
    {
        return new GetResourceByIdParameters
        {
            Id = query.Id,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static UpdateResourceParameters MapToParameters(this UpdateResourceDto update)
    {
        return new UpdateResourceParameters
        {
            Id = update.Id,
            ResourceBranch = update.ResourceBranch,
            ExpiresIn = update.ExpiresIn
        };
    }

    public static GetResourcesByIdsParameters MapToParameters(this ResourcesByIdsQueryDto query)
    {
        return new GetResourcesByIdsParameters
        {
            Ids = query.Ids,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static GetResourceAncestorsParameters MapToParameters(this ResourceAncestorsQueryDto query, int limit)
    {
        return new GetResourceAncestorsParameters
        {
            BranchPath = query.BranchPath,
            LastId = query.LastId,
            Limit = limit,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static GetResourcesByLocationIdParameters MapToParameters(this ResourcesByLocationIdQueryDto query, int limit)
    {
        return new GetResourcesByLocationIdParameters
        {
            LocationId = query.LocationId,
            LastId = query.LastId,
            Limit = limit,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static GetResourcesPageParameters MapToParameters(this ResourcePageQueryDto query, int limit)
    {
        return new GetResourcesPageParameters
        {
            LastId = query.LastId,
            Limit = limit,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static GetResourceSubtreeParameters MapToParameters(this ResourceSubtreeQueryDto query, int limit)
    {
        return new GetResourceSubtreeParameters
        {
            BranchPath = query.BranchPath,
            MaxDepth = query.MaxDepth,
            IncludeSelf = query.IncludeSelf,
            LastId = query.LastId,
            Limit = limit,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static GetResourcesByBranchPatternParameters MapToParameters(
        this ResourceBranchPatternQueryDto query,
        int limit)
    {
        return new GetResourcesByBranchPatternParameters
        {
            Pattern = query.Pattern,
            LastId = query.LastId,
            Limit = limit,
            IncludeExpired = query.IncludeExpired
        };
    }

    public static GetResourceLocationsByIdParameters MapToParameters(this ResourceLocationsQueryDto query, int limit)
    {
        return new GetResourceLocationsByIdParameters
        {
            ResourceId = query.ResourceId,
            LastId = query.LastId,
            Limit = limit,
            IncludeExpired = query.IncludeExpired
        };
    }
}
