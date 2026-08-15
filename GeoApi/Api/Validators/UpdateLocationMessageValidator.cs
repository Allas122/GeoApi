using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class UpdateLocationMessageValidator : AbstractValidator<UpdateLocationMessage>
{
    public UpdateLocationMessageValidator()
    {
        RuleFor(message => message.Point).NotNull();
        RuleFor(message => message.Point!)
            .SetValidator(new PointDtoValidator())
            .When(message => message.Point is not null);
    }
}
