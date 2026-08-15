namespace GeoApi.Application.Dto;

public record ResourceAncestorsQueryDto(string BranchPath, int LastId, int Limit, bool IncludeExpired);
