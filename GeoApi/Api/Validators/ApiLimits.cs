namespace GeoApi.Api.Validators;

public static class ApiLimits
{
    public const int MaxResourcesPerBatch = 500;
    public const int MaxPointsPerBatch = 1000;
    public const int MaxIdsPerQuery = 500;
    public const int MaxGridCells = 10_000;
    public const long MaxExpiresInSeconds = 3_153_600_000;
}
