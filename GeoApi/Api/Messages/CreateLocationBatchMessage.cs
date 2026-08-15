using GeoApi.Api.Dto;

namespace GeoApi.Api.Messages;

public class CreateLocationBatchMessage
{
    public List<PointDto> Points { get; set; } = [];
}
