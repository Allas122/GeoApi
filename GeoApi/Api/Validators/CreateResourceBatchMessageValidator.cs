using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class CreateResourceBatchMessageValidator : AbstractValidator<CreateResourceBatchMessage>
{
    public CreateResourceBatchMessageValidator()
    {
        RuleFor(message => message.Resources)
            .NotEmpty()
            .Must(resources => resources.Count <= ApiLimits.MaxResourcesPerBatch)
            .WithMessage($"'{{PropertyName}}' must not contain more than {ApiLimits.MaxResourcesPerBatch} items.");

        RuleForEach(message => message.Resources).SetValidator(new CreateResourceWithLocationsMessageValidator());
    }
}

public class CreateResourceWithLocationsMessageValidator : AbstractValidator<CreateResourceWithLocationsMessage>
{
    public CreateResourceWithLocationsMessageValidator()
    {
        RuleFor(message => message.ResourceBranch).LtreePath();

        RuleFor(message => message.ExpiresInSeconds)
            .InclusiveBetween(0, ApiLimits.MaxExpiresInSeconds)
            .WithMessage("'{PropertyName}' must be between 0 and " + ApiLimits.MaxExpiresInSeconds +
                         " seconds (0 means the resource never expires).");

        RuleFor(message => message.Points)
            .NotEmpty()
            .Must(points => points.Count <= ApiLimits.MaxPointsPerBatch)
            .WithMessage($"'{{PropertyName}}' must not contain more than {ApiLimits.MaxPointsPerBatch} items.");

        RuleForEach(message => message.Points).SetValidator(new PointDtoValidator());
    }
}
