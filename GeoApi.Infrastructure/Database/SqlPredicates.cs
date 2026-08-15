namespace GeoApi.Infrastructure.Database;

public static class SqlPredicates
{
    public const string NotExpired =
        "(@IncludeExpired OR r.expires_in = '0'::INTERVAL OR r.created_at + r.expires_in > now())";
}
