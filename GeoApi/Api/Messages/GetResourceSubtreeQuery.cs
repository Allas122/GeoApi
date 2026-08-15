namespace GeoApi.Api.Messages;

public class GetResourceSubtreeQuery
{
    public string BranchPath { get; set; } = string.Empty;
    public int? MaxDepth { get; set; }
    public bool IncludeSelf { get; set; } = true;
    public int LastId { get; set; }
    public int Limit { get; set; }
    public bool IncludeExpired { get; set; }
}
