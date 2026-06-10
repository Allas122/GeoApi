using NetTopologySuite.Geometries;

namespace GeoApi.Domain.Dto.Location;

public record GridCluster
{
    public required Point Center { get; set; }
    public int Count { get; set; }
}

public record GridClusterWithResourceIds : GridCluster
{
    int[] ResourceIds { get; set; }
}