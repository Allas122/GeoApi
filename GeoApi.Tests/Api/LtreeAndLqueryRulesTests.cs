using FluentValidation;
using FluentValidation.TestHelper;
using GeoApi.Api.Messages;
using GeoApi.Api.Validators;

namespace GeoApi.Tests.Api;

public class LtreeRulesTests
{
    private class Holder
    {
        public string Path { get; set; } = string.Empty;
    }

    private class HolderValidator : AbstractValidator<Holder>
    {
        public HolderValidator()
        {
            RuleFor(holder => holder.Path).LtreePath();
        }
    }

    private readonly HolderValidator _validator = new();

    [Theory]
    [InlineData("root")]
    [InlineData("root.child")]
    [InlineData("root.child_2.leaf9")]
    public void Accepts_ValidPaths(string path)
    {
        Assert.True(_validator.TestValidate(new Holder { Path = path }).IsValid);
    }

    [Theory]
    [InlineData("root.a\n")]
    [InlineData("root\n")]
    public void Rejects_TrailingNewline(string path)
    {
        Assert.False(_validator.TestValidate(new Holder { Path = path }).IsValid);
    }

    [Fact]
    public void Rejects_LabelLongerThanPostgresLimit()
    {
        string path = new string('a', LtreeRules.MaxLabelLength + 1);

        Assert.False(_validator.TestValidate(new Holder { Path = path }).IsValid);
    }

    [Fact]
    public void Accepts_LabelAtPostgresLimit()
    {
        string path = new string('a', LtreeRules.MaxLabelLength);

        Assert.True(_validator.TestValidate(new Holder { Path = path }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("root..child")]
    [InlineData("root.child-with-dash")]
    [InlineData(".root")]
    [InlineData("root.")]
    [InlineData("root.child;DROP TABLE")]
    public void Rejects_InvalidPaths(string path)
    {
        Assert.False(_validator.TestValidate(new Holder { Path = path }).IsValid);
    }
}

public class LqueryRulesTests
{
    private readonly GetResourcesByBranchPatternQueryValidator _validator = new();

    private TestValidationResult<GetResourcesByBranchPatternQuery> Validate(string pattern)
    {
        return _validator.TestValidate(new GetResourcesByBranchPatternQuery { Pattern = pattern, Limit = 10 });
    }

    [Theory]
    [InlineData("root")]
    [InlineData("root.child")]
    [InlineData("root.*")]
    [InlineData("*.child")]
    [InlineData("root.*{1,3}.leaf")]
    [InlineData("root.a|b")]
    [InlineData("root.!a")]
    [InlineData("root.child@")]
    [InlineData("root.child*")]
    [InlineData("*{2}")]
    public void Accepts_ValidLqueryPatterns(string pattern)
    {
        Assert.True(Validate(pattern).IsValid);
    }

    [Theory]
    [InlineData("***")]
    [InlineData("|")]
    [InlineData("a.")]
    [InlineData(".a")]
    [InlineData("!")]
    [InlineData("a..b")]
    [InlineData("a.b;DROP TABLE")]
    [InlineData("")]
    public void Rejects_BrokenLqueryPatterns(string pattern)
    {
        Assert.False(Validate(pattern).IsValid);
    }

    [Fact]
    public void Rejects_InvertedQuantifier()
    {
        Assert.False(Validate("root.*{3,1}").IsValid);
    }

    [Fact]
    public void Accepts_OrderedQuantifier()
    {
        Assert.True(Validate("root.*{1,3}").IsValid);
    }

    [Fact]
    public void Rejects_PatternLongerThanLimit()
    {
        Assert.False(Validate(new string('a', LqueryRules.MaxPatternLength + 1)).IsValid);
    }
}
