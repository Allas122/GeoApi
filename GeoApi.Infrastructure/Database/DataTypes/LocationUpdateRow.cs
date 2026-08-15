namespace GeoApi.Infrastructure.Database.DataTypes;

public class LocationUpdateRow
{
    public int? TargetId { get; set; }
    public int? ConflictingId { get; set; }
    public int? UpdatedId { get; set; }
    public double? UpdatedLongitude { get; set; }
    public double? UpdatedLatitude { get; set; }
}
