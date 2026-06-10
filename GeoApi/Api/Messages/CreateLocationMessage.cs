using GeoApi.Api.Dto;
using NetTopologySuite.Geometries;

namespace GeoApi.Api.Messages;

public class CreateLocationMessage
{
    public PointDto Point { get; set; }
}