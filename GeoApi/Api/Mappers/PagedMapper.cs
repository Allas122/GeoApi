using GeoApi.Api.Dto;
using GeoApi.Application.Dto;

namespace GeoApi.Api.Mappers;

public static class PagedMapper
{
    public static PagedResponse<TResponse> MapToResponse<TItem, TResponse>(
        this PagedResultDto<TItem> page,
        Func<TItem, TResponse> map)
    {
        return new PagedResponse<TResponse>(
            page.Items.Select(map).ToList(),
            page.NextLastId,
            page.HasMore);
    }
}
