namespace GeoApi.Application.Dto;

public record CreateResourceWithLocationsDto(
    string ResourceBranch,
    TimeSpan ExpiresIn,
    IReadOnlyList<PointDto> Points);
