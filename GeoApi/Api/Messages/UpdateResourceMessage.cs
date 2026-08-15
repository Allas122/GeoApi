namespace GeoApi.Api.Messages;

public class UpdateResourceMessage
{
    public string ResourceBranch { get; set; } = string.Empty;

    public long ExpiresInSeconds { get; set; }
}
