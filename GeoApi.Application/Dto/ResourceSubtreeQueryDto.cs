namespace GeoApi.Application.Dto;

public record ResourceSubtreeQueryDto(
    string BranchPath,
    int? MaxDepth,
    bool IncludeSelf,
    int LastId,
    int Limit,
    bool IncludeExpired);
