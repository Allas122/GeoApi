using System.Text.Json.Serialization;

namespace GeoApi.Api.Dto;

[JsonNumberHandling(JsonNumberHandling.Strict)]
public record PointDto(double Longitude, double Latitude);