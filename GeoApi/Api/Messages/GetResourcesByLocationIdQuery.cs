namespace GeoApi.Api.Messages;

public class GetResourcesByLocationIdQuery
{
    public int LastId { get; set; }
    public int Limit { get; set; }
    public bool IncludeExpired { get; set; }
}
