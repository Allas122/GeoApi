namespace GeoApi.Api.Dto;

public record PagedResponse<T>(IReadOnlyList<T> Items, int? NextLastId, bool HasMore);
