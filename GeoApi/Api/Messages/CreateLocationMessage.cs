using GeoApi.Api.Dto;

namespace GeoApi.Api.Messages;

public class CreateLocationMessage
{
    public PointDto? Point { get; set; }
}
