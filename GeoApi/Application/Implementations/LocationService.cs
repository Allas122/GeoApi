using GeoApi.Application.Abstractions;
using GeoApi.Application.Dto;
using GeoApi.Domain.Repositories;
using NetTopologySuite.Geometries;

namespace GeoApi.Application.Implementations;

public class LocationService(ILocationRepository repository) : ILocationService
{
    private ILocationRepository _locationRepository = repository;

    public Task<IEnumerable<int>> BulkCreateAsync(IEnumerable<Point> points, CancellationToken ct = default)
    {
        return _locationRepository.BulkCreateOrGetAsync(points, ct);
    }

    public Task<int> CreateAsync(Point point, CancellationToken ct = default)
    {
        return _locationRepository.CreateOrGetAsync(point, ct);
    }

    public Task<LocationDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}