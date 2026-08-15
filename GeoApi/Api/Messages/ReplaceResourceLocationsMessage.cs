using GeoApi.Api.Dto;

namespace GeoApi.Api.Messages;

public class ReplaceResourceLocationsMessage
{
    public List<PointDto> Points { get; set; } = [];
}
