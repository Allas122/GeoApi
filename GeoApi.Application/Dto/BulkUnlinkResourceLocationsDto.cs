namespace GeoApi.Application.Dto;

public record BulkUnlinkResourceLocationsDto(int ResourceId, IReadOnlyList<int> LocationIds);
