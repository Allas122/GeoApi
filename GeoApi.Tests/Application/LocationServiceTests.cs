using GeoApi.Application.Dto;
using GeoApi.Application.Implementations;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.ParameterObjects.Location;
using GeoApi.Domain.Repositories;
using Moq;
using DomainPointDto = GeoApi.Domain.Geometry.Coordinate;

namespace GeoApi.Tests.Application;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _repository = new(MockBehavior.Strict);

    private LocationService CreateSut()
    {
        return new LocationService(_repository.Object);
    }

    private static Coordinate Point(double longitude, double latitude)
    {
        return new Coordinate(longitude, latitude);
    }

    [Fact]
    public async Task BulkCreateAsync_WithEmptyInput_DoesNotTouchRepository()
    {
        LocationService sut = CreateSut();

        IReadOnlyList<int> ids = await sut.BulkCreateAsync([]);

        Assert.Empty(ids);
        _repository.Verify(
            r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_PassesPointsThroughAndMaterializesResult()
    {
        _repository
            .Setup(r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([7, 8]);

        LocationService sut = CreateSut();

        IReadOnlyList<int> ids = await sut.BulkCreateAsync([new PointDto(30.5, 50.4), new PointDto(31.0, 51.0)]);

        Assert.Equal([7, 8], ids);
        _repository.Verify(
            r => r.BulkCreateOrGetAsync(
                It.Is<IEnumerable<DomainPointDto>>(points =>
                    points.Count() == 2 &&
                    points.First().Longitude == 30.5 &&
                    points.First().Latitude == 50.4),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryReturnsNull_ThrowsLocationNotFound()
    {
        _repository
            .Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocationEntity?)null);

        LocationService sut = CreateSut();

        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(() => sut.GetByIdAsync(42));

        Assert.Equal(42, exception.LocationId);
    }

    [Fact]
    public async Task GetByIdAsync_MapsGeometryXToLongitudeAndYToLatitude()
    {
        _repository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationEntity { Id = 1, Point = Point(30.5, 50.4) });

        LocationService sut = CreateSut();

        LocationDto location = await sut.GetByIdAsync(1);

        Assert.Equal(1, location.Id);
        Assert.Equal(30.5, location.Point.Longitude);
        Assert.Equal(50.4, location.Point.Latitude);
    }

    [Fact]
    public async Task UpdateAsync_WhenUpdated_ReturnsLocation()
    {
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<UpdateLocationParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationEntity { Id = 5, Point = Point(10.0, 20.0) });

        LocationService sut = CreateSut();

        LocationDto location = await sut.UpdateAsync(new UpdateLocationDto(5, new PointDto(10.0, 20.0)));

        Assert.Equal(5, location.Id);
        Assert.Equal(10.0, location.Point.Longitude);
        Assert.Equal(20.0, location.Point.Latitude);
    }

    [Fact]
    public async Task UpdateAsync_ForwardsIdAndPointToRepository()
    {
        UpdateLocationParameters? captured = null;
        _repository
            .Setup(r => r.UpdateAsync(It.IsAny<UpdateLocationParameters>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateLocationParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(new LocationEntity { Id = 11, Point = Point(-73.9, 40.7) });

        LocationService sut = CreateSut();

        await sut.UpdateAsync(new UpdateLocationDto(11, new PointDto(-73.9, 40.7)));

        Assert.NotNull(captured);
        Assert.Equal(11, captured.Id);
        Assert.Equal(-73.9, captured.Point.Longitude);
        Assert.Equal(40.7, captured.Point.Latitude);
    }

    [Fact]
    public async Task GetInRadiusAsync_RequestsOneExtraRowAndReportsHasMore()
    {
        GetLocationsInRadiusParameters? captured = null;
        _repository
            .Setup(r => r.GetLocationsInRadiusAsync(
                It.IsAny<GetLocationsInRadiusParameters>(),
                It.IsAny<CancellationToken>()))
            .Callback<GetLocationsInRadiusParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync([
                new LocationEntity { Id = 1, Point = Point(1, 1) },
                new LocationEntity { Id = 2, Point = Point(2, 2) },
                new LocationEntity { Id = 3, Point = Point(3, 3) }
            ]);

        LocationService sut = CreateSut();

        PagedResultDto<LocationDto> page = await sut.GetInRadiusAsync(
            new LocationsInRadiusQueryDto(new PointDto(0, 0), 1000, 0, 2));

        Assert.NotNull(captured);
        Assert.Equal(3, captured.Limit);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.Equal(2, page.NextLastId);
    }

    [Fact]
    public async Task GetInRadiusAsync_WhenFewerRowsThanLimit_ReportsNoMore()
    {
        _repository
            .Setup(r => r.GetLocationsInRadiusAsync(
                It.IsAny<GetLocationsInRadiusParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new LocationEntity { Id = 1, Point = Point(1, 1) }]);

        LocationService sut = CreateSut();

        PagedResultDto<LocationDto> page = await sut.GetInRadiusAsync(
            new LocationsInRadiusQueryDto(new PointDto(0, 0), 1000, 0, 10));

        Assert.Single(page.Items);
        Assert.False(page.HasMore);
        Assert.Equal(1, page.NextLastId);
    }
}
