namespace GeoApi.Application.Dto;

public record GridClusterDto(PointDto Center, int Count, int ResourceCount, IReadOnlyList<int> ResourceIds);
