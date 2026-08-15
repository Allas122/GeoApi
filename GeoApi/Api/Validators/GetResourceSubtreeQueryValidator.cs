using FluentValidation;
using GeoApi.Api.Messages;
using GeoApi.Application.Pagination;

namespace GeoApi.Api.Validators;

public class GetResourceSubtreeQueryValidator : AbstractValidator<GetResourceSubtreeQuery>
{
    public GetResourceSubtreeQueryValidator()
    {
        RuleFor(query => query.BranchPath).LtreePath();
        RuleFor(query => query.MaxDepth).GreaterThanOrEqualTo(0).When(query => query.MaxDepth.HasValue);
        RuleFor(query => query.LastId).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Limit).InclusiveBetween(0, PagedResult.MaxLimit);
    }
}
