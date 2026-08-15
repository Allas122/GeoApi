using GeoApi.Api.Dto;
using GeoApi.Api.Mappers;
using GeoApi.Api.Messages;
using GeoApi.Application.Dto;
using GeoApi.Application.Mappers;
using GeoApi.Domain.ParameterObjects.Location;
using GeoApi.Domain.ParameterObjects.Resource;
using ApiPointDto = GeoApi.Api.Dto.PointDto;
using ApplicationPointDto = GeoApi.Application.Dto.PointDto;

namespace GeoApi.Tests.Api;

public class UpdateMappersTests
{
    [Fact]
    public void MapToUpdateDto_TakesIdFromRouteAndConvertsSecondsToTimeSpan()
    {
        var message = new UpdateResourceMessage { ResourceBranch = "root.a", ExpiresInSeconds = 90 };

        UpdateResourceDto dto = message.MapToUpdateDto(42);

        Assert.Equal(42, dto.Id);
        Assert.Equal("root.a", dto.ResourceBranch);
        Assert.Equal(TimeSpan.FromSeconds(90), dto.ExpiresIn);
    }

    [Fact]
    public void MapToReplacementDto_TakesResourceIdFromRouteAndKeepsPointOrder()
    {
        var message = new ReplaceResourceLocationsMessage
        {
            Points = [new ApiPointDto(1, 2), new ApiPointDto(3, 4)]
        };

        ReplaceResourceLocationsDto dto = message.MapToReplacementDto(9);

        Assert.Equal(9, dto.ResourceId);
        Assert.Equal(2, dto.Points.Count);
        Assert.Equal(1, dto.Points[0].Longitude);
        Assert.Equal(2, dto.Points[0].Latitude);
        Assert.Equal(3, dto.Points[1].Longitude);
        Assert.Equal(4, dto.Points[1].Latitude);
    }

    [Fact]
    public void MapToUpdateDto_ForLocation_PreservesLongitudeLatitudeOrder()
    {
        var message = new UpdateLocationMessage { Point = new ApiPointDto(30.5, 50.4) };

        UpdateLocationDto dto = message.MapToUpdateDto(4);

        Assert.Equal(4, dto.Id);
        Assert.Equal(30.5, dto.Point.Longitude);
        Assert.Equal(50.4, dto.Point.Latitude);
    }

    [Fact]
    public void MapToParameters_ForLocationUpdate_PreservesLongitudeLatitudeOrder()
    {
        UpdateLocationParameters parameters = new UpdateLocationDto(4, new ApplicationPointDto(30.5, 50.4))
            .MapToParameters();

        Assert.Equal(4, parameters.Id);
        Assert.Equal(30.5, parameters.Point.Longitude);
        Assert.Equal(50.4, parameters.Point.Latitude);
    }

    [Fact]
    public void MapToParameters_ForResourceUpdate_CopiesEveryField()
    {
        UpdateResourceParameters parameters = new UpdateResourceDto(4, "root.b", TimeSpan.FromHours(1))
            .MapToParameters();

        Assert.Equal(4, parameters.Id);
        Assert.Equal("root.b", parameters.ResourceBranch);
        Assert.Equal(TimeSpan.FromHours(1), parameters.ExpiresIn);
    }

    [Fact]
    public void MapToResponse_ForResource_ConvertsTimeSpanBackToSeconds()
    {
        var dto = new ResourceDto(
            1,
            "root.a",
            new DateTime(2026, 1, 1),
            new DateTime(2026, 1, 2),
            TimeSpan.FromSeconds(120));

        ResourceResponse response = dto.MapToResponse();

        Assert.Equal(1, response.Id);
        Assert.Equal("root.a", response.ResourceBranch);
        Assert.Equal(120, response.ExpiresInSeconds);
    }
}
