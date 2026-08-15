namespace GeoApi.Infrastructure.Database.DataTypes;

public class GridClusterRow
{
    public double CenterLongitude { get; set; }
    public double CenterLatitude { get; set; }
    public int Count { get; set; }
    public int ResourceCount { get; set; }
    public int[] ResourceIds { get; set; } = [];
}
