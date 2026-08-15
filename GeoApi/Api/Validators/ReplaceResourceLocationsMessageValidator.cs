using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class ReplaceResourceLocationsMessageValidator : AbstractValidator<ReplaceResourceLocationsMessage>
{
    public ReplaceResourceLocationsMessageValidator()
    {
        RuleFor(message => message.Points)
            .NotNull()
            .Must(points => points.Count <= ApiLimits.MaxPointsPerBatch)
            .WithMessage($"'{{PropertyName}}' must not contain more than {ApiLimits.MaxPointsPerBatch} items.");

        RuleForEach(message => message.Points).SetValidator(new PointDtoValidator());
    }
}
