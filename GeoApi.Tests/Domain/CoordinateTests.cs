using GeoApi.Domain.Geometry;

namespace GeoApi.Tests.Domain;

public class CoordinateTests
{
    [Theory]
    [InlineData(-180.0, -90.0)]
    [InlineData(180.0, 90.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(37.6208, 55.7539)]
    public void Constructor_AcceptsValuesInsideRange(double longitude, double latitude)
    {
        var coordinate = new Coordinate(longitude, latitude);

        Assert.Equal(longitude, coordinate.Longitude);
        Assert.Equal(latitude, coordinate.Latitude);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_RejectsLongitudeOutsideRange(double longitude)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Coordinate(longitude, 0.0));

        Assert.Equal("longitude", exception.ParamName);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_RejectsLatitudeOutsideRange(double latitude)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Coordinate(0.0, latitude));

        Assert.Equal("latitude", exception.ParamName);
    }

    [Fact]
    public void Coordinates_WithSameValues_AreEqual()
    {
        Assert.Equal(new Coordinate(10.0, 20.0), new Coordinate(10.0, 20.0));
        Assert.NotEqual(new Coordinate(10.0, 20.0), new Coordinate(20.0, 10.0));
    }
}
