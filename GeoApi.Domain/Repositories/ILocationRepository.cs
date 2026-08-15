using GeoApi.Domain.Dto.Location;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.ParameterObjects.Location;

namespace GeoApi.Domain.Repositories;

public interface ILocationRepository
{
    Task<LocationEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<IEnumerable<LocationEntity>> GetLocationsInRadiusAsync(
        GetLocationsInRadiusParameters parameters,
        CancellationToken ct = default);

    Task<int> CreateOrGetAsync(Coordinate location, CancellationToken ct = default);
    Task<IEnumerable<int>> BulkCreateOrGetAsync(IEnumerable<Coordinate> points, CancellationToken ct = default);

    Task<LocationEntity> UpdateAsync(UpdateLocationParameters parameters, CancellationToken ct = default);

    Task<IEnumerable<GridClusterWithResourceIds>> GetWindowedAndClusteredByGridAsync(
        GetWindowedAndClusteredByGridParameters parameters,
        CancellationToken ct = default);
}
