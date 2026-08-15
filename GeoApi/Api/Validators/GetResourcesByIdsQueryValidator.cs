using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class GetResourcesByIdsQueryValidator : AbstractValidator<GetResourcesByIdsQuery>
{
    public GetResourcesByIdsQueryValidator()
    {
        RuleFor(query => query.Ids)
            .NotEmpty()
            .Must(ids => ids.Length <= ApiLimits.MaxIdsPerQuery)
            .WithMessage($"'{{PropertyName}}' must not contain more than {ApiLimits.MaxIdsPerQuery} items.");

        RuleForEach(query => query.Ids).GreaterThan(0);
    }
}
