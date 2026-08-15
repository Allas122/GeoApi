using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class UnlinkResourceLocationsQueryValidator : AbstractValidator<UnlinkResourceLocationsQuery>
{
    public UnlinkResourceLocationsQueryValidator()
    {
        RuleFor(query => query.LocationIds)
            .NotEmpty()
            .Must(ids => ids.Length <= ApiLimits.MaxIdsPerQuery)
            .WithMessage($"'{{PropertyName}}' must not contain more than {ApiLimits.MaxIdsPerQuery} items.");

        RuleForEach(query => query.LocationIds).GreaterThan(0);
    }
}
