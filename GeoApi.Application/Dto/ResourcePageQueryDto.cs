namespace GeoApi.Application.Dto;

public record ResourcePageQueryDto(int LastId, int Limit, bool IncludeExpired);
