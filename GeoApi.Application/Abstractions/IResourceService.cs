using GeoApi.Application.Dto;

namespace GeoApi.Application.Abstractions;

public interface IResourceService
{
    Task<IReadOnlyList<CreatedResourceDto>> CreateBatchAsync(
        IReadOnlyList<CreateResourceWithLocationsDto> resources,
        CancellationToken ct = default);

    Task<ResourceDto> UpdateAsync(UpdateResourceDto update, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    Task LinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default);

    Task UnlinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default);

    Task<IReadOnlyList<int>> BulkUnlinkLocationsAsync(
        BulkUnlinkResourceLocationsDto request,
        CancellationToken ct = default);

    Task<IReadOnlyList<int>> ReplaceLocationsAsync(
        ReplaceResourceLocationsDto replacement,
        CancellationToken ct = default);

    Task<ResourceDto> GetByIdAsync(ResourceByIdQueryDto query, CancellationToken ct = default);

    Task<IReadOnlyList<ResourceDto>> GetByIdsAsync(ResourcesByIdsQueryDto query, CancellationToken ct = default);

    Task<PagedResultDto<ResourceDto>> GetPageAsync(ResourcePageQueryDto query, CancellationToken ct = default);

    Task<PagedResultDto<ResourceDto>> GetSubtreeAsync(ResourceSubtreeQueryDto query, CancellationToken ct = default);

    Task<PagedResultDto<ResourceDto>> GetAncestorsAsync(
        ResourceAncestorsQueryDto query,
        CancellationToken ct = default);

    Task<PagedResultDto<ResourceDto>> GetByBranchPatternAsync(
        ResourceBranchPatternQueryDto query,
        CancellationToken ct = default);

    Task<PagedResultDto<LocationDto>> GetLocationsAsync(
        ResourceLocationsQueryDto query,
        CancellationToken ct = default);

    Task<PagedResultDto<ResourceDto>> GetByLocationIdAsync(
        ResourcesByLocationIdQueryDto query,
        CancellationToken ct = default);
}
