using FluentValidation.TestHelper;
using GeoApi.Api.Messages;
using GeoApi.Api.Validators;

namespace GeoApi.Tests.Api;

public class GetLocationClustersQueryValidatorTests
{
    private readonly GetLocationClustersQueryValidator _validator = new();

    private static GetLocationClustersQuery Query(double gridSize, double minLon = 37.4, double maxLon = 37.8)
    {
        return new GetLocationClustersQuery
        {
            MinLon = minLon,
            MaxLon = maxLon,
            MinLat = 55.6,
            MaxLat = 55.9,
            GridSize = gridSize
        };
    }

    [Fact]
    public void Accepts_WindowWithinCellBudget()
    {
        Assert.True(_validator.TestValidate(Query(0.01)).IsValid);
    }

    [Fact]
    public void Rejects_WholeWorldWithTinyGrid()
    {
        var query = new GetLocationClustersQuery
        {
            MinLon = -180,
            MaxLon = 180,
            MinLat = -90,
            MaxLat = 90,
            GridSize = 0.0001
        };

        Assert.False(_validator.TestValidate(query).IsValid);
    }

    [Fact]
    public void Rejects_GridSizeJustBelowBudget()
    {
        Assert.False(_validator.TestValidate(Query(0.0001)).IsValid);
    }

    [Fact]
    public void Rejects_ZeroGridSizeWithoutDivideByZero()
    {
        TestValidationResult<GetLocationClustersQuery> result = _validator.TestValidate(Query(0));

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(query => query.GridSize);
    }

    [Fact]
    public void Rejects_InvertedWindowWithoutCellCheck()
    {
        Assert.False(_validator.TestValidate(Query(0.01, minLon: 37.8, maxLon: 37.4)).IsValid);
    }

    [Fact]
    public void Accepts_IncludeExpiredFlag()
    {
        var query = Query(0.01);
        query.IncludeExpired = true;

        Assert.True(_validator.TestValidate(query).IsValid);
    }
}
