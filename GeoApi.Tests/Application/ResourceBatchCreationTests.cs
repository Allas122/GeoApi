using GeoApi.Application.Dto;
using GeoApi.Application.Implementations;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Geometry;
using GeoApi.Domain.Repositories;
using Moq;

namespace GeoApi.Tests.Application;

public class ResourceBatchCreationTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<ITransactionScope> _transaction = new(MockBehavior.Strict);
    private readonly Mock<IResourceRepository> _resourceRepository = new(MockBehavior.Strict);
    private readonly Mock<ILocationRepository> _locationRepository = new(MockBehavior.Strict);

    public ResourceBatchCreationTests()
    {
        _transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);
    }

    private ResourceService CreateSut()
    {
        return new ResourceService(_unitOfWork.Object, _resourceRepository.Object, _locationRepository.Object);
    }

    private void SetupBulkCreate(params int[] ids)
    {
        _resourceRepository
            .Setup(r => r.BulkCreateAsync(It.IsAny<IReadOnlyList<ResourceEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);
    }

    private static CreateResourceWithLocationsDto Resource(string branch, params PointDto[] points)
    {
        return new CreateResourceWithLocationsDto(branch, TimeSpan.Zero, points);
    }

    [Fact]
    public async Task CreateBatchAsync_WithNoResources_SkipsTheTransactionEntirely()
    {
        ResourceService sut = CreateSut();

        Assert.Empty(await sut.CreateBatchAsync([]));

        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBatchAsync_WithResourcesButNoPoints_NeverTouchesTheLocationRepository()
    {
        SetupBulkCreate(11, 12);
        _resourceRepository
            .Setup(r => r.BulkLinkPairsAsync(
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        ResourceService sut = CreateSut();

        IReadOnlyList<CreatedResourceDto> created =
            await sut.CreateBatchAsync([Resource("root.a"), Resource("root.b")]);

        Assert.Equal([11, 12], created.Select(resource => resource.ResourceId).ToArray());
        Assert.All(created, resource => Assert.Empty(resource.LocationIds));

        _locationRepository.Verify(
            r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<Coordinate>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateBatchAsync_WhenRepositoryReturnsTooFewResourceIds_Throws()
    {
        SetupBulkCreate(11);

        ResourceService sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateBatchAsync([Resource("root.a"), Resource("root.b")]));

        Assert.Contains("2", exception.Message);
    }

    [Fact]
    public async Task CreateBatchAsync_WhenLocationRepositoryReturnsTooFewIds_Throws()
    {
        SetupBulkCreate(11);
        _locationRepository
            .Setup(r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<Coordinate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([21]);

        ResourceService sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateBatchAsync([Resource("root.a", new PointDto(1.0, 1.0), new PointDto(2.0, 2.0))]));
    }
}
