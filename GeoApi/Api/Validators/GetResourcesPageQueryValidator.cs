using FluentValidation;
using GeoApi.Api.Messages;
using GeoApi.Application.Pagination;

namespace GeoApi.Api.Validators;

public class GetResourcesPageQueryValidator : AbstractValidator<GetResourcesPageQuery>
{
    public GetResourcesPageQueryValidator()
    {
        RuleFor(query => query.LastId).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Limit).InclusiveBetween(0, PagedResult.MaxLimit);
    }
}
