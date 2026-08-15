namespace GeoApi.Application.Dto;

public record LocationClustersQueryDto(
    double MinLon,
    double MinLat,
    double MaxLon,
    double MaxLat,
    double GridSize,
    string? BranchPath,
    bool IncludeExpired);
