namespace GeoApi.Api.Dto;

public record CreatedResourceResponse(
    int ResourceId,
    string ResourceBranch,
    long ExpiresInSeconds,
    IReadOnlyList<int> LocationIds);
