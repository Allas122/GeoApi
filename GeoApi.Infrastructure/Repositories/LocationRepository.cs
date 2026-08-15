using System.Data;
using Dapper;
using GeoApi.Domain.Dto.Location;
using GeoApi.Domain.Entities;
using GeoApi.Domain.Exceptions;
using GeoApi.Domain.ParameterObjects.Location;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Configuration;
using GeoApi.Infrastructure.Database;
using GeoApi.Infrastructure.Database.Abstractions;
using GeoApi.Infrastructure.Database.DataTypes;
using GeoApi.Infrastructure.Database.Parameters;
using GeoApi.Infrastructure.Mappers;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

using Microsoft.Extensions.Logging;

using Coordinate = GeoApi.Domain.Geometry.Coordinate;

namespace GeoApi.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    public const int MaxResourceIdsPerCluster = 100;

    private readonly IDbSession _dbSession;
    private readonly ILogger<LocationRepository> _logger;
    private readonly int? _commandTimeout;

    public LocationRepository(
        IDbSession session,
        ILogger<LocationRepository> logger,
        IOptions<DatabaseOptions> options)
    {
        _dbSession = session;
        _logger = logger;
        _commandTimeout = DbCommands.NormalizeTimeout(options.Value.CommandTimeout);
    }

    public async Task<LocationEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           SELECT l.id                          AS "Id",
                                  ST_X(l.point::geometry)       AS "Longitude",
                                  ST_Y(l.point::geometry)       AS "Latitude"
                           FROM public.locations l
                           WHERE l.id = @Id
                           """;
        var parameters = new
        {
            Id = id
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        LocationRow? row = await lease.QueryFirstOrDefaultAsync<LocationRow>(command);
        return row?.MapToLocation();
    }

    public async Task<IEnumerable<LocationEntity>> GetLocationsInRadiusAsync(
        GetLocationsInRadiusParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);

        const string sql = """
                           SELECT l.id                          AS "Id",
                                  ST_X(l.point::geometry)       AS "Longitude",
                                  ST_Y(l.point::geometry)       AS "Latitude"
                           FROM public.locations l
                           WHERE ST_DWithin(l.point, @Location::geography, @RadiusMeters)
                             AND l.id > @LastId
                           ORDER BY l.id
                           LIMIT @Limit
                           """;

        var sqlParameters = new
        {
            Location = parameters.Center.MapToPoint(),
            parameters.RadiusMeters,
            parameters.LastId,
            parameters.Limit
        };
        var command = CreateCommand(sql, sqlParameters, lease.Transaction, ct);
        return (await lease.QueryAsync<LocationRow>(command)).MapToLocations();
    }

    public async Task<int> CreateOrGetAsync(Coordinate location, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           WITH inserted AS (
                               INSERT INTO public.locations (point)
                               VALUES (@Point::geography)
                               ON CONFLICT (ST_AsBinary(point)) DO NOTHING
                               RETURNING id
                           )
                           SELECT id FROM inserted
                           UNION ALL
                           SELECT id FROM public.locations
                           WHERE ST_AsBinary(point) = ST_AsBinary(@Point::geography)
                           LIMIT 1
                           """;

        var parameters = new
        {
            Point = location.MapToPoint()
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QuerySingleAsync<int>(command);
    }

    public async Task<IEnumerable<int>> BulkCreateOrGetAsync(
        IEnumerable<Coordinate> points,
        CancellationToken ct = default)
    {
        Point[] geometries = points.MapToPoints();
        if (geometries.Length == 0)
        {
            return [];
        }

        _logger.LogDebug("Bulk create or get for {PointCount} points", geometries.Length);

        await using var lease = await _dbSession.LeaseAsync(ct);

        const string sql = """
                           WITH input AS (
                               SELECT p.ord, p.geom::geography AS point
                               FROM UNNEST(@Points) WITH ORDINALITY AS p(geom, ord)
                           ),
                           deduplicated AS (
                               SELECT DISTINCT ON (ST_AsBinary(point)) point
                               FROM input
                               ORDER BY ST_AsBinary(point)
                           ),
                           inserted AS (
                               INSERT INTO public.locations (point)
                               SELECT point FROM deduplicated
                               ON CONFLICT (ST_AsBinary(point)) DO NOTHING
                               RETURNING id, point
                           ),
                           resolved AS (
                               SELECT id, point FROM inserted
                               UNION ALL
                               SELECT l.id, l.point
                               FROM public.locations l
                               JOIN deduplicated d ON ST_AsBinary(l.point) = ST_AsBinary(d.point)
                               WHERE NOT EXISTS (
                                   SELECT 1 FROM inserted i WHERE ST_AsBinary(i.point) = ST_AsBinary(l.point)
                               )
                           )
                           SELECT rs.id
                           FROM input i
                           JOIN resolved rs ON ST_AsBinary(rs.point) = ST_AsBinary(i.point)
                           ORDER BY i.ord
                           """;

        var parameters = new
        {
            Points = new ArrayParameter<Point>(geometries)
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<int>(command);
    }

    public async Task<LocationEntity> UpdateAsync(
        UpdateLocationParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);

        const string sql = """
                           WITH target AS (
                               SELECT id FROM public.locations WHERE id = @Id
                           ),
                           conflicting AS (
                               SELECT id FROM public.locations
                               WHERE ST_AsBinary(point) = ST_AsBinary(@Point::geography)
                                 AND id <> @Id
                           ),
                           updated AS (
                               UPDATE public.locations
                               SET point = @Point::geography
                               WHERE id = @Id
                                 AND NOT EXISTS (SELECT 1 FROM conflicting)
                               RETURNING id, point
                           )
                           SELECT (SELECT id FROM target)         AS "TargetId",
                                  (SELECT id FROM conflicting)    AS "ConflictingId",
                                  (SELECT id FROM updated)                              AS "UpdatedId",
                                  (SELECT ST_X(point::geometry) FROM updated)           AS "UpdatedLongitude",
                                  (SELECT ST_Y(point::geometry) FROM updated)           AS "UpdatedLatitude"
                           """;

        var sqlParameters = new
        {
            parameters.Id,
            Point = parameters.Point.MapToPoint()
        };
        var command = CreateCommand(sql, sqlParameters, lease.Transaction, ct);
        LocationUpdateRow row = await lease.QuerySingleAsync<LocationUpdateRow>(command);

        if (row.TargetId is null)
        {
            throw new LocationNotFoundException(parameters.Id);
        }

        if (row.ConflictingId is not null)
        {
            throw new LocationPointConflictException(row.ConflictingId.Value);
        }

        return new LocationEntity
        {
            Id = row.UpdatedId!.Value,
            Point = new Coordinate(row.UpdatedLongitude!.Value, row.UpdatedLatitude!.Value)
        };
    }

    public async Task<IEnumerable<GridClusterWithResourceIds>> GetWindowedAndClusteredByGridAsync(
        GetWindowedAndClusteredByGridParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);

        string sql = $"""
                            SELECT ST_X(ST_Centroid(ST_Collect(DISTINCT l.point::geometry))) AS "CenterLongitude",
                                   ST_Y(ST_Centroid(ST_Collect(DISTINCT l.point::geometry))) AS "CenterLatitude",
                                   COUNT(DISTINCT l.id)::int                                 AS "Count",
                                   COUNT(DISTINCT r.id)::int                                 AS "ResourceCount",
                                   (ARRAY_AGG(DISTINCT r.id))[1:{MaxResourceIdsPerCluster}]  AS "ResourceIds"
                            FROM public.locations l
                            INNER JOIN public.resource_location rl ON rl.location_id = l.id
                            INNER JOIN public.resources r          ON r.id = rl.resource_id
                            WHERE l.point && ST_MakeEnvelope(@MinLon, @MinLat, @MaxLon, @MaxLat, 4326)::geography
                              AND (@BranchPath::ltree IS NULL OR r.resource_branch <@ @BranchPath::ltree)
                              AND {SqlPredicates.NotExpired}
                            GROUP BY ST_SnapToGrid(l.point::geometry, @GridSize)
                            ORDER BY "CenterLongitude", "CenterLatitude"
                            """;

        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        var rows = await lease.QueryAsync<GridClusterRow>(command);
        return rows.Select(LocationMapper.MapToGridCluster);
    }

    private CommandDefinition CreateCommand(
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return DbCommands.Create(sql, parameters, transaction, _commandTimeout, ct);
    }
}
