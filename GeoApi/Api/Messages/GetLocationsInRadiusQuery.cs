namespace GeoApi.Api.Messages;

public class GetLocationsInRadiusQuery
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double RadiusMeters { get; set; }
    public int LastId { get; set; }
    public int Limit { get; set; }
}
