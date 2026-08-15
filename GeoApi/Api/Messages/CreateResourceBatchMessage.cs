using GeoApi.Api.Dto;

namespace GeoApi.Api.Messages;

public class CreateResourceBatchMessage
{
    public List<CreateResourceWithLocationsMessage> Resources { get; set; } = [];
}

public class CreateResourceWithLocationsMessage
{
    public string ResourceBranch { get; set; } = string.Empty;

    public long ExpiresInSeconds { get; set; }

    public List<PointDto> Points { get; set; } = [];
}
