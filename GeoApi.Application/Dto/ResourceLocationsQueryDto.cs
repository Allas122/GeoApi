namespace GeoApi.Application.Dto;

public record ResourceLocationsQueryDto(int ResourceId, int LastId, int Limit, bool IncludeExpired);
