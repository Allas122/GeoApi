using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;

namespace GeoApi.Domain.Repositories;

public interface IResourceRepository
{
    Task<ResourceEntity?> GetByIdAsync(GetResourceByIdParameters parameters, CancellationToken ct = default);
    Task<IEnumerable<ResourceEntity>> GetByIdsAsync(GetResourcesByIdsParameters parameters, CancellationToken ct = default);
    Task<IEnumerable<ResourceEntity>> GetPageAsync(GetResourcesPageParameters parameters, CancellationToken ct = default);

    Task<IEnumerable<ResourceEntity>> GetSubtreeAsync(GetResourceSubtreeParameters parameters, CancellationToken ct = default);
    Task<IEnumerable<ResourceEntity>> GetAncestorsAsync(
        GetResourceAncestorsParameters parameters,
        CancellationToken ct = default);
    Task<IEnumerable<ResourceEntity>> GetByBranchPatternAsync(
        GetResourcesByBranchPatternParameters parameters,
        CancellationToken ct = default);

    Task<IEnumerable<LocationEntity>> GetLocationsAsync(
        GetResourceLocationsByIdParameters parameters,
        CancellationToken ct = default);
    Task<IEnumerable<ResourceEntity>> GetByLocationIdAsync(
        GetResourcesByLocationIdParameters parameters,
        CancellationToken ct = default);

    Task<int> CreateAsync(ResourceEntity resource, CancellationToken ct = default);
    Task<IReadOnlyList<int>> BulkCreateAsync(IReadOnlyList<ResourceEntity> resources, CancellationToken ct = default);
    Task<ResourceEntity?> UpdateAsync(UpdateResourceParameters parameters, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);

    Task<bool> LinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default);
    Task<IEnumerable<int>> BulkLinkLocationsAsync(int resourceId, IEnumerable<int> locationIds, CancellationToken ct = default);
    Task<int> BulkLinkPairsAsync(
        IReadOnlyList<int> resourceIds,
        IReadOnlyList<int> locationIds,
        CancellationToken ct = default);
    Task<bool> UnlinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default);
    Task<IEnumerable<int>> BulkUnlinkLocationsAsync(int resourceId, IEnumerable<int> locationIds, CancellationToken ct = default);
    Task<int> UnlinkAllLocationsAsync(int resourceId, CancellationToken ct = default);
}
