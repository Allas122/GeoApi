using GeoApi.Application.Dto;
using GeoApi.Application.Pagination;

namespace GeoApi.Tests.Application;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, PagedResult.DefaultLimit)]
    [InlineData(-5, PagedResult.DefaultLimit)]
    [InlineData(1, 1)]
    [InlineData(PagedResult.MaxLimit, PagedResult.MaxLimit)]
    [InlineData(PagedResult.MaxLimit + 1, PagedResult.MaxLimit)]
    [InlineData(int.MaxValue, PagedResult.MaxLimit)]
    public void NormalizeLimit_ClampsToConfiguredRange(int input, int expected)
    {
        Assert.Equal(expected, PagedResult.NormalizeLimit(input));
    }

    [Fact]
    public void Create_WhenFetchedExceedsLimit_TrimsAndFlagsHasMore()
    {
        PagedResultDto<int> page = PagedResult.Create([1, 2, 3, 4], 3, item => item);

        Assert.Equal([1, 2, 3], page.Items);
        Assert.True(page.HasMore);
        Assert.Equal(3, page.NextLastId);
    }

    [Fact]
    public void Create_WhenFetchedEqualsLimit_DoesNotFlagHasMore()
    {
        PagedResultDto<int> page = PagedResult.Create([1, 2, 3], 3, item => item);

        Assert.Equal([1, 2, 3], page.Items);
        Assert.False(page.HasMore);
        Assert.Equal(3, page.NextLastId);
    }

    [Fact]
    public void Create_WhenEmpty_ReturnsNullCursor()
    {
        PagedResultDto<int> page = PagedResult.Create<int>([], 10, item => item);

        Assert.Empty(page.Items);
        Assert.False(page.HasMore);
        Assert.Null(page.NextLastId);
    }

    [Fact]
    public void Create_TakesCursorFromLastRetainedItem()
    {
        PagedResultDto<int> page = PagedResult.Create([10, 20, 30, 40, 50], 2, item => item);

        Assert.Equal([10, 20], page.Items);
        Assert.Equal(20, page.NextLastId);
        Assert.True(page.HasMore);
    }
}
