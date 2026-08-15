namespace GeoApi.Application.Dto;

public record UpdateResourceDto(int Id, string ResourceBranch, TimeSpan ExpiresIn);
