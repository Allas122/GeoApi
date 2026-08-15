using FluentValidation;

namespace GeoApi.Api.Validators;

public static class LqueryRules
{
    public const int MaxPatternLength = 1024;
    public const int MaxLevelCount = 256;

    private const string Label = @"[A-Za-z0-9_]+[@*%]*";
    private const string LabelAlternatives = $"!?{Label}(\\|{Label})*";
    private const string AnyLevel = @"\*(\{\d+(,\d+)?\})?";
    private const string Level = $"({AnyLevel}|{LabelAlternatives})";
    private const string LqueryPattern = $@"\A{Level}(\.{Level})*\z";

    public static IRuleBuilderOptions<T, string> Lquery<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(MaxPatternLength)
            .Matches(LqueryPattern)
            .WithMessage(
                "'{PropertyName}' must be a valid ltree query (labels of A-Z, a-z, 0-9, _ separated by '.', " +
                "optionally using '*', '!', '|', '@', '%' and '{n,m}').")
            .Must(pattern => pattern.Split('.').Length <= MaxLevelCount)
            .WithMessage($"'{{PropertyName}}' must not contain more than {MaxLevelCount} levels.")
            .Must(HasValidQuantifiers)
            .WithMessage("'{PropertyName}' must not contain a '{n,m}' quantifier with n greater than m.");
    }

    private static bool HasValidQuantifiers(string pattern)
    {
        foreach (string level in pattern.Split('.'))
        {
            int open = level.IndexOf('{');
            if (open < 0 || !level.EndsWith('}'))
            {
                continue;
            }

            string[] bounds = level[(open + 1)..^1].Split(',');
            if (bounds.Length != 2)
            {
                continue;
            }

            if (int.TryParse(bounds[0], out int low) && int.TryParse(bounds[1], out int high) && low > high)
            {
                return false;
            }
        }

        return true;
    }
}
