using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class CreateLocationMessageValidator : AbstractValidator<CreateLocationMessage>
{
    public CreateLocationMessageValidator()
    {
        RuleFor(message => message.Point).NotNull();
        RuleFor(message => message.Point!)
            .SetValidator(new PointDtoValidator())
            .When(message => message.Point is not null);
    }
}
