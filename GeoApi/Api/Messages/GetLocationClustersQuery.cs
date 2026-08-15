namespace GeoApi.Api.Messages;

public class GetLocationClustersQuery
{
    public double MinLon { get; set; }
    public double MinLat { get; set; }
    public double MaxLon { get; set; }
    public double MaxLat { get; set; }
    public double GridSize { get; set; }
    public string? BranchPath { get; set; }
    public bool IncludeExpired { get; set; }
}
