using FluentValidation;
using GeoApi.Api.Messages;
using GeoApi.Application.Pagination;

namespace GeoApi.Api.Validators;

public class GetResourcesByBranchPatternQueryValidator : AbstractValidator<GetResourcesByBranchPatternQuery>
{
    public GetResourcesByBranchPatternQueryValidator()
    {
        RuleFor(query => query.Pattern).Lquery();
        RuleFor(query => query.LastId).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Limit).InclusiveBetween(0, PagedResult.MaxLimit);
    }
}
