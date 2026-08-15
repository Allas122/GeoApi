namespace GeoApi.Api.Messages;

public class GetResourcesByIdsQuery
{
    public int[] Ids { get; set; } = [];

    public bool IncludeExpired { get; set; }
}
