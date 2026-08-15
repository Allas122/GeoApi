namespace GeoApi.Domain.ParameterObjects.Resource;

public record UpdateResourceParameters
{
    public required int Id { get; set; }
    public required string ResourceBranch { get; set; }
    public TimeSpan ExpiresIn { get; set; }
}
