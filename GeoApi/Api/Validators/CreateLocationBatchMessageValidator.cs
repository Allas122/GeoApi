using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class CreateLocationBatchMessageValidator : AbstractValidator<CreateLocationBatchMessage>
{
    public CreateLocationBatchMessageValidator()
    {
        RuleFor(message => message.Points)
            .NotEmpty()
            .Must(points => points.Count <= ApiLimits.MaxPointsPerBatch)
            .WithMessage($"'{{PropertyName}}' must not contain more than {ApiLimits.MaxPointsPerBatch} items.");

        RuleForEach(message => message.Points).SetValidator(new PointDtoValidator());
    }
}
