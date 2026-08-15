namespace GeoApi.Domain.Geometry;

public readonly record struct Coordinate
{
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;
    public const double MinLatitude = -90;
    public const double MaxLatitude = 90;

    public Coordinate(double longitude, double latitude)
    {
        if (longitude is < MinLongitude or > MaxLongitude || double.IsNaN(longitude))
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                longitude,
                $"Longitude must be between {MinLongitude} and {MaxLongitude}.");
        }

        if (latitude is < MinLatitude or > MaxLatitude || double.IsNaN(latitude))
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                latitude,
                $"Latitude must be between {MinLatitude} and {MaxLatitude}.");
        }

        Longitude = longitude;
        Latitude = latitude;
    }

    public double Longitude { get; }
    public double Latitude { get; }
}
