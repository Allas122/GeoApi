using FluentValidation;

namespace GeoApi.Api.Validators;

public static class LtreeRules
{
    public const int MaxLabelCount = 256;
    public const int MaxPathLength = 1024;
    public const int MaxLabelLength = 1000;

    private const string LabelPathPattern = @"\A[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)*\z";

    public static IRuleBuilderOptions<T, string> LtreePath<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(MaxPathLength)
            .Matches(LabelPathPattern)
            .WithMessage("'{PropertyName}' must be a valid ltree path (labels of A-Z, a-z, 0-9, _ separated by '.').")
            .Must(path => path.Split('.').Length <= MaxLabelCount)
            .WithMessage($"'{{PropertyName}}' must not contain more than {MaxLabelCount} labels.")
            .Must(path => path.Split('.').All(label => label.Length <= MaxLabelLength))
            .WithMessage($"'{{PropertyName}}' must not contain a label longer than {MaxLabelLength} characters.");
    }
}
