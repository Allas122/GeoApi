namespace GeoApi.Application.Dto;

public record ResourceDto(
    int Id,
    string ResourceBranch,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TimeSpan ExpiresIn);
