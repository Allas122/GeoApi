using GeoApi.Domain.Dto.Location;
using NetTopologySuite.Geometries;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects;
using GeoApi.Domain.ParameterObjects.Location;

namespace GeoApi.Domain.Repositories;

public interface ILocationRepository
{
    public Task<LocationEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    public Task<IEnumerable<LocationEntity>> GetLocationsInRadiusAsync(GetLocationsInRadiusParameters parameters, CancellationToken ct);
    public Task<int> CreateOrGetAsync(Point location, CancellationToken ct = default);
    public Task<IEnumerable<int>> BulkCreateOrGetAsync(IEnumerable<Point> points, CancellationToken ct = default);
    public Task<IEnumerable<GridClusterWithResourceIds>> GetWindowedAndClusteredByGrid(
        GetWindowedAndClusteredByGridParameters parameters,
        CancellationToken ct = default
        );
}