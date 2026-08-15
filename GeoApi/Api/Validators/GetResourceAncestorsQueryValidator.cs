using FluentValidation;
using GeoApi.Api.Messages;
using GeoApi.Application.Pagination;

namespace GeoApi.Api.Validators;

public class GetResourceAncestorsQueryValidator : AbstractValidator<GetResourceAncestorsQuery>
{
    public GetResourceAncestorsQueryValidator()
    {
        RuleFor(query => query.BranchPath).LtreePath();
        RuleFor(query => query.LastId).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Limit).InclusiveBetween(0, PagedResult.MaxLimit);
    }
}
