namespace GeoApi.Application.Dto;

public record LocationsInRadiusQueryDto(PointDto Center, double RadiusMeters, int LastId, int Limit);
