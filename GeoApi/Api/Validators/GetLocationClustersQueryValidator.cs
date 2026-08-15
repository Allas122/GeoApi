using FluentValidation;
using GeoApi.Api.Messages;

namespace GeoApi.Api.Validators;

public class GetLocationClustersQueryValidator : AbstractValidator<GetLocationClustersQuery>
{
    public GetLocationClustersQueryValidator()
    {
        RuleFor(query => query.MinLon).InclusiveBetween(-180, 180);
        RuleFor(query => query.MaxLon).InclusiveBetween(-180, 180);
        RuleFor(query => query.MinLat).InclusiveBetween(-90, 90);
        RuleFor(query => query.MaxLat).InclusiveBetween(-90, 90);
        RuleFor(query => query.MaxLon).GreaterThan(query => query.MinLon);
        RuleFor(query => query.MaxLat).GreaterThan(query => query.MinLat);
        RuleFor(query => query.GridSize).GreaterThan(0);
        RuleFor(query => query.BranchPath!).LtreePath().When(query => query.BranchPath is not null);

        RuleFor(query => query)
            .Must(query => CountCells(query) <= ApiLimits.MaxGridCells)
            .WithMessage(
                $"The requested window and 'GridSize' produce more than {ApiLimits.MaxGridCells} grid cells. " +
                "Narrow the window or increase 'GridSize'.")
            .When(IsWindowUsable);
    }

    private static bool IsWindowUsable(GetLocationClustersQuery query)
    {
        return query.GridSize > 0 && query.MaxLon > query.MinLon && query.MaxLat > query.MinLat;
    }

    private static double CountCells(GetLocationClustersQuery query)
    {
        double columns = (query.MaxLon - query.MinLon) / query.GridSize;
        double rows = (query.MaxLat - query.MinLat) / query.GridSize;
        return columns * rows;
    }
}
