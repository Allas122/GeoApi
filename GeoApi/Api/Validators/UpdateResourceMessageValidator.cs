using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class UpdateResourceMessageValidator : AbstractValidator<UpdateResourceMessage>
{
    public UpdateResourceMessageValidator()
    {
        RuleFor(message => message.ResourceBranch).LtreePath();

        RuleFor(message => message.ExpiresInSeconds)
            .InclusiveBetween(0, ApiLimits.MaxExpiresInSeconds)
            .WithMessage("'{PropertyName}' must be between 0 and " + ApiLimits.MaxExpiresInSeconds +
                         " seconds (0 means the resource never expires).");
    }
}
