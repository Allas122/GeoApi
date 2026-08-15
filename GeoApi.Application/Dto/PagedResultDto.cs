namespace GeoApi.Application.Dto;

public record PagedResultDto<T>(IReadOnlyList<T> Items, int? NextLastId, bool HasMore);
