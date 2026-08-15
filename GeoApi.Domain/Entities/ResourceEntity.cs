namespace GeoApi.Domain.Entities;

public class ResourceEntity
{
    public int Id { get; set; }
    public required string ResourceBranch { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan ExpiresIn { get; set; }
}