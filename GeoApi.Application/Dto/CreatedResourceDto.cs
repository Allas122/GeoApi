namespace GeoApi.Application.Dto;

public record CreatedResourceDto(
    int ResourceId,
    string ResourceBranch,
    TimeSpan ExpiresIn,
    IReadOnlyList<int> LocationIds);
