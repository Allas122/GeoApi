namespace GeoApi.Application.Dto;

public record ResourceBranchPatternQueryDto(string Pattern, int LastId, int Limit, bool IncludeExpired);
