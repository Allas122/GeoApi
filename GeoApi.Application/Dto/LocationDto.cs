namespace GeoApi.Application.Dto;

public class LocationDto
{
    public int Id { get; set; }
    public required PointDto Point { get; set; }
}
