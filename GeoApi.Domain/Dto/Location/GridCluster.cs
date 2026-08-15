using GeoApi.Domain.Geometry;

namespace GeoApi.Domain.Dto.Location;

public record GridClusterWithResourceIds
{
    public required Coordinate Center { get; set; }
    public int Count { get; set; }
    public int ResourceCount { get; set; }
    public required int[] ResourceIds { get; set; }
}
