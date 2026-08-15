using GeoApi.Application.Dto;

namespace GeoApi.Application.Abstractions;

public interface ILocationService
{
    Task<IReadOnlyList<int>> BulkCreateAsync(IReadOnlyList<PointDto> points, CancellationToken ct = default);
    Task<int> CreateAsync(PointDto point, CancellationToken ct = default);

    Task<LocationDto> GetByIdAsync(int id, CancellationToken ct = default);

    Task<LocationDto> UpdateAsync(UpdateLocationDto update, CancellationToken ct = default);

    Task<PagedResultDto<LocationDto>> GetInRadiusAsync(
        LocationsInRadiusQueryDto query,
        CancellationToken ct = default);

    Task<IReadOnlyList<GridClusterDto>> GetClustersAsync(
        LocationClustersQueryDto query,
        CancellationToken ct = default);
}
