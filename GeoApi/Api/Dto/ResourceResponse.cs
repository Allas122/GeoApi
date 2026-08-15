namespace GeoApi.Api.Dto;

public record ResourceResponse(
    int Id,
    string ResourceBranch,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long ExpiresInSeconds);
