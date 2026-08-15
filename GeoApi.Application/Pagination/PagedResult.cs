using GeoApi.Application.Dto;

namespace GeoApi.Application.Pagination;

public static class PagedResult
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 500;

    public static int NormalizeLimit(int limit)
    {
        return limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
    }

    public static PagedResultDto<T> Create<T>(IEnumerable<T> fetched, int limit, Func<T, int> idSelector)
    {
        List<T> items = fetched.ToList();
        bool hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveRange(limit, items.Count - limit);
        }

        int? nextLastId = items.Count > 0 ? idSelector(items[^1]) : null;
        return new PagedResultDto<T>(items, nextLastId, hasMore);
    }
}
