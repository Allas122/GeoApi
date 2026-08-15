using GeoApi.Application.Dto;
using GeoApi.Application.Implementations;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;
using Moq;

using GeoApi.Domain.Geometry;

namespace GeoApi.Tests.Application;

public class ResourceLinkServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<ITransactionScope> _transaction = new(MockBehavior.Strict);
    private readonly Mock<IResourceRepository> _resourceRepository = new(MockBehavior.Strict);
    private readonly Mock<ILocationRepository> _locationRepository = new(MockBehavior.Strict);

    public ResourceLinkServiceTests()
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

    private static ResourceEntity Resource(int id)
    {
        return new ResourceEntity
        {
            Id = id,
            ResourceBranch = "root.a",
            CreatedAt = new DateTime(2026, 1, 1),
            UpdatedAt = new DateTime(2026, 1, 1),
            ExpiresIn = TimeSpan.Zero
        };
    }

    private void SetupResourceExists(int id, bool exists)
    {
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<GetResourceByIdParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists ? Resource(id) : null);
    }

    private void SetupLocationExists(int id, bool exists)
    {
        _locationRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists
                ? new LocationEntity { Id = id, Point = new Coordinate(1, 2) }
                : null);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeleted_Completes()
    {
        _resourceRepository
            .Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateSut().DeleteAsync(7);
    }

    [Fact]
    public async Task DeleteAsync_WhenNothingDeleted_ThrowsResourceNotFound()
    {
        _resourceRepository
            .Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => CreateSut().DeleteAsync(7));

        Assert.Equal(7, exception.ResourceId);
    }

    [Fact]
    public async Task LinkLocationAsync_WhenResourceMissing_ThrowsResourceNotFound()
    {
        SetupResourceExists(7, false);

        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => CreateSut().LinkLocationAsync(7, 11));

        Assert.Equal(7, exception.ResourceId);
        _resourceRepository.Verify(
            r => r.LinkLocationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LinkLocationAsync_WhenLocationMissing_ThrowsLocationNotFound()
    {
        SetupResourceExists(7, true);
        SetupLocationExists(11, false);

        var exception = await Assert.ThrowsAsync<LocationNotFoundException>(
            () => CreateSut().LinkLocationAsync(7, 11));

        Assert.Equal(11, exception.LocationId);
        _resourceRepository.Verify(
            r => r.LinkLocationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LinkLocationAsync_WhenBothExist_CommitsRegardlessOfPriorLink(bool newlyLinked)
    {
        SetupResourceExists(7, true);
        SetupLocationExists(11, true);
        _resourceRepository
            .Setup(r => r.LinkLocationAsync(7, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newlyLinked);

        await CreateSut().LinkLocationAsync(7, 11);

        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlinkLocationAsync_WhenResourceMissing_ThrowsResourceNotFound()
    {
        SetupResourceExists(7, false);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CreateSut().UnlinkLocationAsync(7, 11));

        _resourceRepository.Verify(
            r => r.UnlinkLocationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlinkLocationAsync_WhenLinkAbsent_ThrowsLinkNotFound()
    {
        SetupResourceExists(7, true);
        _resourceRepository
            .Setup(r => r.UnlinkLocationAsync(7, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<ResourceLocationLinkNotFoundException>(
            () => CreateSut().UnlinkLocationAsync(7, 11));

        Assert.Equal(7, exception.ResourceId);
        Assert.Equal(11, exception.LocationId);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnlinkLocationAsync_WhenRemoved_Commits()
    {
        SetupResourceExists(7, true);
        _resourceRepository
            .Setup(r => r.UnlinkLocationAsync(7, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateSut().UnlinkLocationAsync(7, 11);

        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUnlinkLocationsAsync_WhenResourceMissing_ThrowsWithoutCommit()
    {
        SetupResourceExists(7, false);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => CreateSut().BulkUnlinkLocationsAsync(new BulkUnlinkResourceLocationsDto(7, [11, 12])));

        _resourceRepository.Verify(
            r => r.BulkUnlinkLocationsAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUnlinkLocationsAsync_ReturnsOnlyActuallyUnlinkedIds()
    {
        SetupResourceExists(7, true);
        _resourceRepository
            .Setup(r => r.BulkUnlinkLocationsAsync(7, It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([11]);

        IReadOnlyList<int> unlinked = await CreateSut()
            .BulkUnlinkLocationsAsync(new BulkUnlinkResourceLocationsDto(7, [11, 12]));

        Assert.Equal([11], unlinked);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUnlinkLocationsAsync_WithEmptyList_SkipsRepositoryAndCommits()
    {
        SetupResourceExists(7, true);

        IReadOnlyList<int> unlinked = await CreateSut()
            .BulkUnlinkLocationsAsync(new BulkUnlinkResourceLocationsDto(7, []));

        Assert.Empty(unlinked);
        _resourceRepository.Verify(
            r => r.BulkUnlinkLocationsAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LinkLocationAsync_ChecksResourceIncludingExpired()
    {
        GetResourceByIdParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<GetResourceByIdParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GetResourceByIdParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync((ResourceEntity?)null);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CreateSut().LinkLocationAsync(7, 11));

        Assert.NotNull(captured);
        Assert.Equal(7, captured.Id);
        Assert.True(captured.IncludeExpired);
    }
}
