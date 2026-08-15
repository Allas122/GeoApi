using GeoApi.Application.Dto;
using GeoApi.Application.Implementations;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;
using Moq;
using DomainPointDto = GeoApi.Domain.Geometry.Coordinate;

namespace GeoApi.Tests.Application;

public class ResourceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<ITransactionScope> _transaction = new(MockBehavior.Strict);
    private readonly Mock<IResourceRepository> _resourceRepository = new(MockBehavior.Strict);
    private readonly Mock<ILocationRepository> _locationRepository = new(MockBehavior.Strict);

    public ResourceServiceTests()
    {
        _transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);
    }

    private ResourceService CreateSut()
    {
        return new ResourceService(_unitOfWork.Object, _resourceRepository.Object, _locationRepository.Object);
    }

    private static ResourceEntity Resource(int id, string branch = "root.a", int expiresInSeconds = 0)
    {
        return new ResourceEntity
        {
            Id = id,
            ResourceBranch = branch,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            UpdatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified),
            ExpiresIn = TimeSpan.FromSeconds(expiresInSeconds)
        };
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReturnsNull_ThrowsResourceNotFound()
    {
        _resourceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UpdateResourceParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceEntity?)null);

        ResourceService sut = CreateSut();

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => sut.UpdateAsync(new UpdateResourceDto(1, "root.a", TimeSpan.Zero)));

        Assert.Equal(1, exception.ResourceId);
    }

    [Fact]
    public async Task UpdateAsync_ForwardsAllFieldsAndMapsResult()
    {
        UpdateResourceParameters? captured = null;
        _resourceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UpdateResourceParameters>(), It.IsAny<CancellationToken>()))
            .Callback<UpdateResourceParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(Resource(3, "root.moved", 60));

        ResourceService sut = CreateSut();

        ResourceDto resource = await sut.UpdateAsync(
            new UpdateResourceDto(3, "root.moved", TimeSpan.FromSeconds(60)));

        Assert.NotNull(captured);
        Assert.Equal(3, captured.Id);
        Assert.Equal("root.moved", captured.ResourceBranch);
        Assert.Equal(TimeSpan.FromSeconds(60), captured.ExpiresIn);

        Assert.Equal(3, resource.Id);
        Assert.Equal("root.moved", resource.ResourceBranch);
        Assert.Equal(TimeSpan.FromSeconds(60), resource.ExpiresIn);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotOpenTransaction()
    {
        _resourceRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UpdateResourceParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Resource(1));

        ResourceService sut = CreateSut();

        await sut.UpdateAsync(new UpdateResourceDto(1, "root.a", TimeSpan.Zero));

        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceLocationsAsync_WhenResourceMissing_ThrowsAndDoesNotCommit()
    {
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<GetResourceByIdParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceEntity?)null);

        ResourceService sut = CreateSut();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => sut.ReplaceLocationsAsync(new ReplaceResourceLocationsDto(7, [new PointDto(1, 1)])));

        _resourceRepository.Verify(
            r => r.UnlinkAllLocationsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task ReplaceLocationsAsync_LooksUpResourceIncludingExpired()
    {
        GetResourceByIdParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<GetResourceByIdParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GetResourceByIdParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync((ResourceEntity?)null);

        ResourceService sut = CreateSut();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => sut.ReplaceLocationsAsync(new ReplaceResourceLocationsDto(7, [])));

        Assert.NotNull(captured);
        Assert.Equal(7, captured.Id);
        Assert.True(captured.IncludeExpired);
    }

    [Fact]
    public async Task ReplaceLocationsAsync_UnlinksOldLinksThenCreatesAndLinksNewOnes()
    {
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<GetResourceByIdParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Resource(7));
        _resourceRepository
            .Setup(r => r.UnlinkAllLocationsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _locationRepository
            .Setup(r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([11, 12]);
        _resourceRepository
            .Setup(r => r.BulkLinkLocationsAsync(7, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([11, 12]);

        ResourceService sut = CreateSut();

        IReadOnlyList<int> result = await sut.ReplaceLocationsAsync(
            new ReplaceResourceLocationsDto(7, [new PointDto(30.5, 50.4), new PointDto(31.0, 51.0)]));

        Assert.Equal([11, 12], result);
        _resourceRepository.Verify(r => r.UnlinkAllLocationsAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        _resourceRepository.Verify(
            r => r.BulkLinkLocationsAsync(
                7,
                It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 11, 12 })),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceLocationsAsync_WithEmptyPoints_ClearsLinksWithoutCreatingLocations()
    {
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<GetResourceByIdParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Resource(7));
        _resourceRepository
            .Setup(r => r.UnlinkAllLocationsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        ResourceService sut = CreateSut();

        IReadOnlyList<int> result = await sut.ReplaceLocationsAsync(new ReplaceResourceLocationsDto(7, []));

        Assert.Empty(result);
        _resourceRepository.Verify(r => r.UnlinkAllLocationsAsync(7, It.IsAny<CancellationToken>()), Times.Once);
        _locationRepository.Verify(
            r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _resourceRepository.Verify(
            r => r.BulkLinkLocationsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_WithEmptyInput_DoesNotOpenTransaction()
    {
        ResourceService sut = CreateSut();

        IReadOnlyList<CreatedResourceDto> created = await sut.CreateBatchAsync([]);

        Assert.Empty(created);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBatchAsync_CreatesResourceAndLinksItsLocations()
    {
        _resourceRepository
            .Setup(r => r.BulkCreateAsync(It.IsAny<IReadOnlyList<ResourceEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([21]);
        _locationRepository
            .Setup(r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([31]);
        _resourceRepository
            .Setup(r => r.BulkLinkPairsAsync(
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        ResourceService sut = CreateSut();

        IReadOnlyList<CreatedResourceDto> created = await sut.CreateBatchAsync([
            new CreateResourceWithLocationsDto("root.a", TimeSpan.FromMinutes(5), [new PointDto(1, 2)])
        ]);

        CreatedResourceDto single = Assert.Single(created);
        Assert.Equal(21, single.ResourceId);
        Assert.Equal("root.a", single.ResourceBranch);
        Assert.Equal(TimeSpan.FromMinutes(5), single.ExpiresIn);
        Assert.Equal([31], single.LocationIds);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_IssuesThreeRoundTripsRegardlessOfBatchSize()
    {
        _resourceRepository
            .Setup(r => r.BulkCreateAsync(It.IsAny<IReadOnlyList<ResourceEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);
        _locationRepository
            .Setup(r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([11, 12, 13, 14]);
        _resourceRepository
            .Setup(r => r.BulkLinkPairsAsync(
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        ResourceService sut = CreateSut();

        await sut.CreateBatchAsync([
            new CreateResourceWithLocationsDto("a", TimeSpan.Zero, [new PointDto(1, 1)]),
            new CreateResourceWithLocationsDto("b", TimeSpan.Zero, [new PointDto(2, 2), new PointDto(3, 3)]),
            new CreateResourceWithLocationsDto("c", TimeSpan.Zero, [new PointDto(4, 4)])
        ]);

        _resourceRepository.Verify(
            r => r.BulkCreateAsync(It.IsAny<IReadOnlyList<ResourceEntity>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _locationRepository.Verify(
            r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _resourceRepository.Verify(
            r => r.BulkLinkPairsAsync(
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_SplitsFlatLocationIdsBackToTheirResources()
    {
        List<int>? capturedResourceIds = null;
        List<int>? capturedLocationIds = null;
        _resourceRepository
            .Setup(r => r.BulkCreateAsync(It.IsAny<IReadOnlyList<ResourceEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);
        _locationRepository
            .Setup(r => r.BulkCreateOrGetAsync(It.IsAny<IEnumerable<DomainPointDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([11, 12, 13, 14]);
        _resourceRepository
            .Setup(r => r.BulkLinkPairsAsync(
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<IReadOnlyList<int>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<int>, IReadOnlyList<int>, CancellationToken>(
                (resourceIds, locationIds, _) =>
                {
                    capturedResourceIds = resourceIds.ToList();
                    capturedLocationIds = locationIds.ToList();
                })
            .ReturnsAsync(4);

        ResourceService sut = CreateSut();

        IReadOnlyList<CreatedResourceDto> created = await sut.CreateBatchAsync([
            new CreateResourceWithLocationsDto("a", TimeSpan.Zero, [new PointDto(1, 1)]),
            new CreateResourceWithLocationsDto("b", TimeSpan.Zero, [new PointDto(2, 2), new PointDto(3, 3)]),
            new CreateResourceWithLocationsDto("c", TimeSpan.Zero, [new PointDto(4, 4)])
        ]);

        Assert.Equal([11], created[0].LocationIds);
        Assert.Equal([12, 13], created[1].LocationIds);
        Assert.Equal([14], created[2].LocationIds);

        Assert.Equal([1, 2, 2, 3], capturedResourceIds);
        Assert.Equal([11, 12, 13, 14], capturedLocationIds);
    }

    [Fact]
    public async Task GetPageAsync_TrimsExtraRowAndReportsHasMore()
    {
        GetResourcesPageParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetPageAsync(It.IsAny<GetResourcesPageParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GetResourcesPageParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync([Resource(1), Resource(2), Resource(3)]);

        ResourceService sut = CreateSut();

        PagedResultDto<ResourceDto> page = await sut.GetPageAsync(new ResourcePageQueryDto(0, 2, false));

        Assert.NotNull(captured);
        Assert.Equal(3, captured.Limit);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.Equal(2, page.NextLastId);
    }

    [Fact]
    public async Task GetByIdsAsync_WithEmptyIds_DoesNotTouchRepository()
    {
        ResourceService sut = CreateSut();

        IReadOnlyList<ResourceDto> resources = await sut.GetByIdsAsync(new ResourcesByIdsQueryDto([], false));

        Assert.Empty(resources);
        _resourceRepository.Verify(
            r => r.GetByIdsAsync(It.IsAny<GetResourcesByIdsParameters>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
