namespace GeoApi.Api.Messages;

public class GetResourcesByBranchPatternQuery
{
    public string Pattern { get; set; } = string.Empty;
    public int LastId { get; set; }
    public int Limit { get; set; }
    public bool IncludeExpired { get; set; }
}
