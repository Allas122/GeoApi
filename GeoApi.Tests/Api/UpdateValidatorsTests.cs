using FluentValidation.TestHelper;
using GeoApi.Api.Dto;
using GeoApi.Api.Messages;
using GeoApi.Api.Validators;

namespace GeoApi.Tests.Api;

public class UpdateResourceMessageValidatorTests
{
    private readonly UpdateResourceMessageValidator _validator = new();

    [Theory]
    [InlineData("root")]
    [InlineData("root.child")]
    [InlineData("root.child_2.leaf")]
    public void Accepts_ValidLtreePaths(string branch)
    {
        TestValidationResult<UpdateResourceMessage> result =
            _validator.TestValidate(new UpdateResourceMessage { ResourceBranch = branch });

        result.ShouldNotHaveValidationErrorFor(message => message.ResourceBranch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("root..child")]
    [InlineData("root.child-with-dash")]
    [InlineData("root.child;DROP TABLE")]
    [InlineData(".root")]
    public void Rejects_InvalidLtreePaths(string branch)
    {
        TestValidationResult<UpdateResourceMessage> result =
            _validator.TestValidate(new UpdateResourceMessage { ResourceBranch = branch });

        result.ShouldHaveValidationErrorFor(message => message.ResourceBranch);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ApiLimits.MaxExpiresInSeconds + 1)]
    public void Rejects_ExpiresInOutsideRange(long seconds)
    {
        TestValidationResult<UpdateResourceMessage> result = _validator.TestValidate(
            new UpdateResourceMessage { ResourceBranch = "root", ExpiresInSeconds = seconds });

        result.ShouldHaveValidationErrorFor(message => message.ExpiresInSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(ApiLimits.MaxExpiresInSeconds)]
    public void Accepts_ExpiresInInsideRange(long seconds)
    {
        TestValidationResult<UpdateResourceMessage> result = _validator.TestValidate(
            new UpdateResourceMessage { ResourceBranch = "root", ExpiresInSeconds = seconds });

        result.ShouldNotHaveValidationErrorFor(message => message.ExpiresInSeconds);
    }
}

public class UpdateLocationMessageValidatorTests
{
    private readonly UpdateLocationMessageValidator _validator = new();

    [Fact]
    public void Rejects_MissingPoint()
    {
        TestValidationResult<UpdateLocationMessage> result =
            _validator.TestValidate(new UpdateLocationMessage { Point = null });

        result.ShouldHaveValidationErrorFor(message => message.Point);
    }

    [Theory]
    [InlineData(180.1, 0)]
    [InlineData(-180.1, 0)]
    [InlineData(0, 90.1)]
    [InlineData(0, -90.1)]
    public void Rejects_OutOfRangeCoordinates(double longitude, double latitude)
    {
        TestValidationResult<UpdateLocationMessage> result = _validator.TestValidate(
            new UpdateLocationMessage { Point = new PointDto(longitude, latitude) });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(180, 90)]
    [InlineData(-180, -90)]
    [InlineData(30.5234, 50.4501)]
    public void Accepts_ValidCoordinates(double longitude, double latitude)
    {
        TestValidationResult<UpdateLocationMessage> result = _validator.TestValidate(
            new UpdateLocationMessage { Point = new PointDto(longitude, latitude) });

        Assert.True(result.IsValid);
    }
}

public class ReplaceResourceLocationsMessageValidatorTests
{
    private readonly ReplaceResourceLocationsMessageValidator _validator = new();

    [Fact]
    public void Accepts_EmptyPointsAsAnExplicitClear()
    {
        TestValidationResult<ReplaceResourceLocationsMessage> result =
            _validator.TestValidate(new ReplaceResourceLocationsMessage { Points = [] });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_MoreThanBatchLimit()
    {
        List<PointDto> points = Enumerable
            .Range(0, ApiLimits.MaxPointsPerBatch + 1)
            .Select(i => new PointDto(i % 180, i % 90))
            .ToList();

        TestValidationResult<ReplaceResourceLocationsMessage> result =
            _validator.TestValidate(new ReplaceResourceLocationsMessage { Points = points });

        result.ShouldHaveValidationErrorFor(message => message.Points);
    }

    [Fact]
    public void Rejects_InvalidCoordinateInsideCollection()
    {
        TestValidationResult<ReplaceResourceLocationsMessage> result = _validator.TestValidate(
            new ReplaceResourceLocationsMessage { Points = [new PointDto(0, 0), new PointDto(0, 91)] });

        Assert.False(result.IsValid);
    }
}
