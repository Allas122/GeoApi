namespace GeoApi.Domain.ParameterObjects.Location;

public record GetWindowedAndClusteredByGridParameters : IWindowed, IExpiryFiltered
{
    public bool IncludeExpired { get; set; }

    public double MinLat { get; set; }
    public double MinLon { get; set; }
    public double MaxLat { get; set; }
    public double MaxLon { get; set; }
    
    public string? BranchPath { get; set; }
    public double GridSize { get; set; }
}