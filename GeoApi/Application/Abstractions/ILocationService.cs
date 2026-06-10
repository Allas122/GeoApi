using GeoApi.Application.Dto;
using NetTopologySuite.Geometries;

namespace GeoApi.Application.Abstractions;

public interface ILocationService
{
    public Task<IEnumerable<int>> BulkCreateAsync(IEnumerable<Point> points, CancellationToken ct = default);
    public Task<int> CreateAsync(Point point, CancellationToken ct = default);
    public Task<LocationDto> GetByIdAsync(int id, CancellationToken ct = default);
}