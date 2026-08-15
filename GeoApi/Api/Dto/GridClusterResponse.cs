namespace GeoApi.Api.Dto;

public record GridClusterResponse(PointDto Center, int Count, int ResourceCount, IReadOnlyList<int> ResourceIds);
