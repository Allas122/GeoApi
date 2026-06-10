using GeoApi.Application.Dto;
using GeoApi.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace GeoApi.Application.Mappers;

[Mapper]
public static partial class LocationMapper
{
    public static partial LocationEntity MapToLocationEntity(this LocationDto locationDto);
}