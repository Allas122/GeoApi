using GeoApi.Api.Dto;

namespace GeoApi.Api.Messages;

public class UpdateLocationMessage
{
    public PointDto? Point { get; set; }
}
