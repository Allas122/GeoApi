using GeoApi.Application.Dto;
using GeoApi.Application.Implementations;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;
using Moq;

namespace GeoApi.Tests.Application;

public class ResourcePaginationTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IResourceRepository> _resourceRepository = new(MockBehavior.Strict);
    private readonly Mock<ILocationRepository> _locationRepository = new(MockBehavior.Strict);

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

    [Fact]
    public async Task GetAncestorsAsync_RequestsOneExtraRowAndTrims()
    {
        GetResourceAncestorsParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetAncestorsAsync(It.IsAny<GetResourceAncestorsParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GetResourceAncestorsParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync([Resource(1), Resource(2), Resource(3)]);

        PagedResultDto<ResourceDto> page = await CreateSut()
            .GetAncestorsAsync(new ResourceAncestorsQueryDto("root.a.b", 0, 2, false));

        Assert.NotNull(captured);
        Assert.Equal(3, captured.Limit);
        Assert.Equal("root.a.b", captured.BranchPath);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.Equal(2, page.NextLastId);
    }

    [Fact]
    public async Task GetAncestorsAsync_ForwardsCursor()
    {
        GetResourceAncestorsParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetAncestorsAsync(It.IsAny<GetResourceAncestorsParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GetResourceAncestorsParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync([]);

        await CreateSut().GetAncestorsAsync(new ResourceAncestorsQueryDto("root", 42, 10, true));

        Assert.NotNull(captured);
        Assert.Equal(42, captured.LastId);
        Assert.True(captured.IncludeExpired);
    }

    [Fact]
    public async Task GetAncestorsAsync_NormalizesZeroLimitToDefault()
    {
        GetResourceAncestorsParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetAncestorsAsync(It.IsAny<GetResourceAncestorsParameters>(), It.IsAny<CancellationToken>()))
            .Callback<GetResourceAncestorsParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync([]);

        await CreateSut().GetAncestorsAsync(new ResourceAncestorsQueryDto("root", 0, 0, false));

        Assert.NotNull(captured);
        Assert.Equal(101, captured.Limit);
    }

    [Fact]
    public async Task GetByLocationIdAsync_RequestsOneExtraRowAndTrims()
    {
        GetResourcesByLocationIdParameters? captured = null;
        _resourceRepository
            .Setup(r => r.GetByLocationIdAsync(
                It.IsAny<GetResourcesByLocationIdParameters>(),
                It.IsAny<CancellationToken>()))
            .Callback<GetResourcesByLocationIdParameters, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync([Resource(5), Resource(6), Resource(7)]);

        PagedResultDto<ResourceDto> page = await CreateSut()
            .GetByLocationIdAsync(new ResourcesByLocationIdQueryDto(11, 0, 2, false));

        Assert.NotNull(captured);
        Assert.Equal(11, captured.LocationId);
        Assert.Equal(3, captured.Limit);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasMore);
        Assert.Equal(6, page.NextLastId);
    }

    [Fact]
    public async Task GetByLocationIdAsync_WhenFewerRowsThanLimit_ReportsNoMore()
    {
        _resourceRepository
            .Setup(r => r.GetByLocationIdAsync(
                It.IsAny<GetResourcesByLocationIdParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([Resource(5)]);

        PagedResultDto<ResourceDto> page = await CreateSut()
            .GetByLocationIdAsync(new ResourcesByLocationIdQueryDto(11, 0, 10, false));

        Assert.Single(page.Items);
        Assert.False(page.HasMore);
        Assert.Equal(5, page.NextLastId);
    }
}
