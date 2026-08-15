using FluentValidation;
using GeoApi.Api.Dto;

namespace GeoApi.Api.Validators;

public class PointDtoValidator : AbstractValidator<PointDto>
{
    public PointDtoValidator()
    {
        RuleFor(point => point.Longitude).InclusiveBetween(-180, 180);
        RuleFor(point => point.Latitude).InclusiveBetween(-90, 90);
    }
}
