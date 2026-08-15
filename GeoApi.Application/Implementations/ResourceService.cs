using GeoApi.Application.Abstractions;
using GeoApi.Application.Dto;
using GeoApi.Application.Mappers;
using GeoApi.Application.Pagination;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;

namespace GeoApi.Application.Implementations;

public class ResourceService(
    IUnitOfWork unitOfWork,
    IResourceRepository resourceRepository,
    ILocationRepository locationRepository) : IResourceService
{
    public async Task<IReadOnlyList<CreatedResourceDto>> CreateBatchAsync(
        IReadOnlyList<CreateResourceWithLocationsDto> resources,
        CancellationToken ct = default)
    {
        if (resources.Count == 0)
        {
            return [];
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        IReadOnlyList<int> resourceIds = await resourceRepository.BulkCreateAsync(
            resources
                .Select(resource => new ResourceEntity
                {
                    ResourceBranch = resource.ResourceBranch,
                    ExpiresIn = resource.ExpiresIn
                })
                .ToArray(),
            ct);

        if (resourceIds.Count != resources.Count)
        {
            throw new InvalidOperationException(
                $"Expected {resources.Count} created resource ids but got {resourceIds.Count}.");
        }

        PointDto[] allPoints = resources.SelectMany(resource => resource.Points).ToArray();
        int[] allLocationIds = allPoints.Length == 0
            ? []
            : (await locationRepository.BulkCreateOrGetAsync(allPoints.MapToCoordinates(), ct)).ToArray();

        if (allLocationIds.Length != allPoints.Length)
        {
            throw new InvalidOperationException(
                $"Expected {allPoints.Length} location ids but got {allLocationIds.Length}.");
        }

        var pairResourceIds = new List<int>(allLocationIds.Length);
        var pairLocationIds = new List<int>(allLocationIds.Length);
        var created = new List<CreatedResourceDto>(resources.Count);

        int offset = 0;
        for (int i = 0; i < resources.Count; i++)
        {
            CreateResourceWithLocationsDto resource = resources[i];
            int[] locationIds = allLocationIds[offset..(offset + resource.Points.Count)];
            offset += resource.Points.Count;

            foreach (int locationId in locationIds)
            {
                pairResourceIds.Add(resourceIds[i]);
                pairLocationIds.Add(locationId);
            }

            created.Add(new CreatedResourceDto(
                resourceIds[i],
                resource.ResourceBranch,
                resource.ExpiresIn,
                locationIds));
        }

        await resourceRepository.BulkLinkPairsAsync(pairResourceIds, pairLocationIds, ct);

        await transaction.CommitAsync(ct);
        return created;
    }

    public async Task<ResourceDto> UpdateAsync(UpdateResourceDto update, CancellationToken ct = default)
    {
        ResourceEntity? resource = await resourceRepository.UpdateAsync(update.MapToParameters(), ct);
        if (resource is null)
        {
            throw new ResourceNotFoundException(update.Id);
        }

        return resource.MapToResourceDto();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        if (!await resourceRepository.DeleteAsync(id, ct))
        {
            throw new ResourceNotFoundException(id);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task LinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        await EnsureResourceExistsAsync(resourceId, ct);

        if (await locationRepository.GetByIdAsync(locationId, ct) is null)
        {
            throw new LocationNotFoundException(locationId);
        }

        await resourceRepository.LinkLocationAsync(resourceId, locationId, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task UnlinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        await EnsureResourceExistsAsync(resourceId, ct);

        if (!await resourceRepository.UnlinkLocationAsync(resourceId, locationId, ct))
        {
            throw new ResourceLocationLinkNotFoundException(resourceId, locationId);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<int>> BulkUnlinkLocationsAsync(
        BulkUnlinkResourceLocationsDto request,
        CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        await EnsureResourceExistsAsync(request.ResourceId, ct);

        int[] unlinked = request.LocationIds.Count == 0
            ? []
            : (await resourceRepository.BulkUnlinkLocationsAsync(request.ResourceId, request.LocationIds, ct))
            .ToArray();

        await transaction.CommitAsync(ct);
        return unlinked;
    }

    private async Task EnsureResourceExistsAsync(int resourceId, CancellationToken ct)
    {
        ResourceEntity? resource = await resourceRepository.GetByIdAsync(
            new GetResourceByIdParameters
            {
                Id = resourceId,
                IncludeExpired = true
            },
            ct);

        if (resource is null)
        {
            throw new ResourceNotFoundException(resourceId);
        }
    }

    public async Task<IReadOnlyList<int>> ReplaceLocationsAsync(
        ReplaceResourceLocationsDto replacement,
        CancellationToken ct = default)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        await EnsureResourceExistsAsync(replacement.ResourceId, ct);

        await resourceRepository.UnlinkAllLocationsAsync(replacement.ResourceId, ct);

        int[] locationIds = replacement.Points.Count == 0
            ? []
            : (await locationRepository.BulkCreateOrGetAsync(replacement.Points.MapToCoordinates(), ct)).ToArray();

        if (locationIds.Length > 0)
        {
            await resourceRepository.BulkLinkLocationsAsync(replacement.ResourceId, locationIds, ct);
        }

        await transaction.CommitAsync(ct);
        return locationIds;
    }

    public async Task<ResourceDto> GetByIdAsync(ResourceByIdQueryDto query, CancellationToken ct = default)
    {
        ResourceEntity? resource = await resourceRepository.GetByIdAsync(query.MapToParameters(), ct);
        if (resource is null)
        {
            throw new ResourceNotFoundException(query.Id);
        }

        return resource.MapToResourceDto();
    }

    public async Task<IReadOnlyList<ResourceDto>> GetByIdsAsync(
        ResourcesByIdsQueryDto query,
        CancellationToken ct = default)
    {
        if (query.Ids.Count == 0)
        {
            return [];
        }

        IEnumerable<ResourceEntity> resources = await resourceRepository.GetByIdsAsync(query.MapToParameters(), ct);
        return resources.MapToResourceDtos().ToList();
    }

    public async Task<PagedResultDto<ResourceDto>> GetPageAsync(
        ResourcePageQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetResourcesPageParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<ResourceEntity> resources = await resourceRepository.GetPageAsync(parameters, ct);
        return PagedResult.Create(resources.MapToResourceDtos(), limit, resource => resource.Id);
    }

    public async Task<PagedResultDto<ResourceDto>> GetSubtreeAsync(
        ResourceSubtreeQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetResourceSubtreeParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<ResourceEntity> resources = await resourceRepository.GetSubtreeAsync(parameters, ct);
        return PagedResult.Create(resources.MapToResourceDtos(), limit, resource => resource.Id);
    }

    public async Task<PagedResultDto<ResourceDto>> GetAncestorsAsync(
        ResourceAncestorsQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetResourceAncestorsParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<ResourceEntity> resources = await resourceRepository.GetAncestorsAsync(parameters, ct);
        return PagedResult.Create(resources.MapToResourceDtos(), limit, resource => resource.Id);
    }

    public async Task<PagedResultDto<ResourceDto>> GetByBranchPatternAsync(
        ResourceBranchPatternQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetResourcesByBranchPatternParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<ResourceEntity> resources = await resourceRepository.GetByBranchPatternAsync(parameters, ct);
        return PagedResult.Create(resources.MapToResourceDtos(), limit, resource => resource.Id);
    }

    public async Task<PagedResultDto<LocationDto>> GetLocationsAsync(
        ResourceLocationsQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetResourceLocationsByIdParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<LocationEntity> locations = await resourceRepository.GetLocationsAsync(parameters, ct);
        return PagedResult.Create(locations.MapToLocationDtos(), limit, location => location.Id);
    }

    public async Task<PagedResultDto<ResourceDto>> GetByLocationIdAsync(
        ResourcesByLocationIdQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetResourcesByLocationIdParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<ResourceEntity> resources = await resourceRepository.GetByLocationIdAsync(parameters, ct);
        return PagedResult.Create(resources.MapToResourceDtos(), limit, resource => resource.Id);
    }
}
