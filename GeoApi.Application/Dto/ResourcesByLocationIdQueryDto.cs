namespace GeoApi.Application.Dto;

public record ResourcesByLocationIdQueryDto(int LocationId, int LastId, int Limit, bool IncludeExpired);
