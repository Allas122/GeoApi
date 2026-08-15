using GeoApi.Application.Abstractions;
using GeoApi.Application.Dto;
using GeoApi.Application.Mappers;
using GeoApi.Application.Pagination;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.ParameterObjects.Location;
using GeoApi.Domain.Repositories;
using GridClusterWithResourceIds = GeoApi.Domain.Dto.Location.GridClusterWithResourceIds;

namespace GeoApi.Application.Implementations;

public class LocationService(ILocationRepository repository) : ILocationService
{
    public async Task<IReadOnlyList<int>> BulkCreateAsync(
        IReadOnlyList<PointDto> points,
        CancellationToken ct = default)
    {
        if (points.Count == 0)
        {
            return [];
        }

        IEnumerable<int> ids = await repository.BulkCreateOrGetAsync(points.MapToCoordinates(), ct);
        return ids.ToList();
    }

    public Task<int> CreateAsync(PointDto point, CancellationToken ct = default)
    {
        return repository.CreateOrGetAsync(point.MapToCoordinate(), ct);
    }

    public async Task<LocationDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        LocationEntity? location = await repository.GetByIdAsync(id, ct);
        if (location is null)
        {
            throw new LocationNotFoundException(id);
        }

        return location.MapToLocationDto();
    }

    public async Task<LocationDto> UpdateAsync(UpdateLocationDto update, CancellationToken ct = default)
    {
        LocationEntity location = await repository.UpdateAsync(update.MapToParameters(), ct);
        return location.MapToLocationDto();
    }

    public async Task<PagedResultDto<LocationDto>> GetInRadiusAsync(
        LocationsInRadiusQueryDto query,
        CancellationToken ct = default)
    {
        int limit = PagedResult.NormalizeLimit(query.Limit);
        GetLocationsInRadiusParameters parameters = query.MapToParameters(limit + 1);

        IEnumerable<LocationEntity> locations = await repository.GetLocationsInRadiusAsync(parameters, ct);
        return PagedResult.Create(locations.MapToLocationDtos(), limit, location => location.Id);
    }

    public async Task<IReadOnlyList<GridClusterDto>> GetClustersAsync(
        LocationClustersQueryDto query,
        CancellationToken ct = default)
    {
        GetWindowedAndClusteredByGridParameters parameters = query.MapToParameters();

        IEnumerable<GridClusterWithResourceIds> clusters =
            await repository.GetWindowedAndClusteredByGridAsync(parameters, ct);
        return clusters.MapToGridClusterDtos().ToList();
    }
}
