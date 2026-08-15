namespace GeoApi.Application.Dto;

public record ResourcesByIdsQueryDto(IReadOnlyList<int> Ids, bool IncludeExpired);
