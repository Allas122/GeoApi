using FluentValidation;
using GeoApi.Api.Messages;
using GeoApi.Application.Pagination;

namespace GeoApi.Api.Validators;

public class GetLocationsInRadiusQueryValidator : AbstractValidator<GetLocationsInRadiusQuery>
{
    public GetLocationsInRadiusQueryValidator()
    {
        RuleFor(query => query.Longitude).InclusiveBetween(-180, 180);
        RuleFor(query => query.Latitude).InclusiveBetween(-90, 90);
        RuleFor(query => query.RadiusMeters).GreaterThan(0);
        RuleFor(query => query.LastId).GreaterThanOrEqualTo(0);
        RuleFor(query => query.Limit).InclusiveBetween(0, PagedResult.MaxLimit);
    }
}
