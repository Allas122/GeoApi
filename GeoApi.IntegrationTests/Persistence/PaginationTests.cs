using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;

namespace GeoApi.IntegrationTests.Persistence;

[Collection(GeoApiCollection.Name)]
public class PaginationTests(GeoApiFixture fixture) : IntegrationTest(fixture)
{
    private async Task<IReadOnlyList<int>> SeedAsync(params string[] branches)
    {
        return await Resources.BulkCreateAsync(
            branches.Select(branch => new ResourceEntity { ResourceBranch = branch, ExpiresIn = TimeSpan.Zero })
                .ToArray());
    }

    [Fact]
    public async Task GetPageAsync_WalksEveryRowExactlyOnceAcrossPages()
    {
        IReadOnlyList<int> ids = await SeedAsync(
            Enumerable.Range(0, 25).Select(i => $"page.n{i}").ToArray());

        var seen = new List<int>();
        int lastId = 0;

        while (true)
        {
            ResourceEntity[] page = (await Resources.GetPageAsync(new GetResourcesPageParameters
            {
                LastId = lastId,
                Limit = 7
            })).ToArray();

            if (page.Length == 0)
            {
                break;
            }

            seen.AddRange(page.Select(resource => resource.Id));
            lastId = page[^1].Id;
        }

        Assert.Equal(ids.Order(), seen.Order());
        Assert.Equal(seen.Count, seen.Distinct().Count());
    }

    [Fact]
    public async Task GetAncestorsAsync_IsPagedAndOrderedById()
    {
        await SeedAsync("a", "a.b", "a.b.c", "a.b.c.d", "unrelated");

        ResourceEntity[] first = (await Resources.GetAncestorsAsync(new GetResourceAncestorsParameters
        {
            BranchPath = "a.b.c.d",
            Limit = 2
        })).ToArray();

        Assert.Equal(2, first.Length);

        ResourceEntity[] second = (await Resources.GetAncestorsAsync(new GetResourceAncestorsParameters
        {
            BranchPath = "a.b.c.d",
            LastId = first[^1].Id,
            Limit = 10
        })).ToArray();

        string[] all = first.Concat(second).Select(resource => resource.ResourceBranch).ToArray();

        Assert.Equal(["a", "a.b", "a.b.c", "a.b.c.d"], all);
    }

    [Fact]
    public async Task GetSubtreeAsync_RespectsMaxDepthAndIncludeSelf()
    {
        await SeedAsync("root", "root.a", "root.a.b", "root.a.b.c");

        ResourceEntity[] withoutSelf = (await Resources.GetSubtreeAsync(new GetResourceSubtreeParameters
        {
            BranchPath = "root",
            MaxDepth = 1,
            IncludeSelf = false,
            Limit = 50
        })).ToArray();

        Assert.Equal(["root.a"], withoutSelf.Select(r => r.ResourceBranch).ToArray());

        ResourceEntity[] withSelf = (await Resources.GetSubtreeAsync(new GetResourceSubtreeParameters
        {
            BranchPath = "root",
            MaxDepth = 1,
            IncludeSelf = true,
            Limit = 50
        })).ToArray();

        Assert.Equal(["root", "root.a"], withSelf.Select(r => r.ResourceBranch).ToArray());
    }

    [Fact]
    public async Task GetByBranchPatternAsync_MatchesLqueryWildcards()
    {
        await SeedAsync("shop.moscow.food", "shop.spb.food", "office.moscow.food");

        ResourceEntity[] matched = (await Resources.GetByBranchPatternAsync(
            new GetResourcesByBranchPatternParameters { Pattern = "shop.*.food", Limit = 50 })).ToArray();

        Assert.Equal(
            ["shop.moscow.food", "shop.spb.food"],
            matched.Select(r => r.ResourceBranch).Order().ToArray());
    }
}
