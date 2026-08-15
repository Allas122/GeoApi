namespace GeoApi.Api.Messages;

public class GetResourceAncestorsQuery
{
    public string BranchPath { get; set; } = string.Empty;
    public int LastId { get; set; }
    public int Limit { get; set; }
    public bool IncludeExpired { get; set; }
}
