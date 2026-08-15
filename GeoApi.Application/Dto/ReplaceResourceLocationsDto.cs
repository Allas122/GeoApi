namespace GeoApi.Application.Dto;

public record ReplaceResourceLocationsDto(int ResourceId, IReadOnlyList<PointDto> Points);
