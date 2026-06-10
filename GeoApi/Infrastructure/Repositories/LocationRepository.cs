using System.Data;
using Dapper;
using GeoApi.Domain.Dto.Location;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Location;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Database.Abstractions;
using NetTopologySuite.Geometries;

namespace GeoApi.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<LocationRepository> _logger;

    public LocationRepository(IDbConnectionFactory factory, ILogger<LocationRepository> logger)
    {
        _dbConnectionFactory = factory;
        _logger = logger;
    }

    public async Task<LocationEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);
        const string sql = """SELECT * FROM locations WHERE id = @Id""";
        var parameters = new
        {
            Id = id
        };
        var command = CreateCommand(sql, parameters, ct: ct);
        return await connection.QueryFirstOrDefaultAsync<LocationEntity>(command);
    }

    public async Task<IEnumerable<LocationEntity>> GetLocationsInRadiusAsync(
        GetLocationsInRadiusParameters parameters,
        CancellationToken ct)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);
        const string sql = """
                            SELECT l.id, l.point
                            FROM public.locations l
                            WHERE ST_DWithin(l.point, @Point, @RadiusMeters)
                              AND l.id > @LastId
                            ORDER BY l.id
                            LIMIT @Limit
                            """;
        
        var command = CreateCommand(sql, parameters, ct: ct);
        return await connection.QueryAsync<LocationEntity>(command);
    }

    public async Task<int> CreateOrGetAsync(Point point, CancellationToken ct = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);
        const string sql = """
                           WITH existing AS (
                               SELECT id FROM public.locations 
                               WHERE ST_AsBinary(point) = ST_AsBinary(@Point)
                               LIMIT 1
                           ),
                           inserted AS (
                               INSERT INTO public.locations (point)
                               SELECT @Point
                               WHERE NOT EXISTS (SELECT 1 FROM existing)
                               RETURNING id
                           )
                           SELECT id FROM inserted
                           UNION ALL
                           SELECT id FROM existing;
                           """;
        var parameters = new
        {
            Point = point,
        };
        var command = CreateCommand(sql, parameters, ct: ct);
        return await connection.QuerySingleAsync<int>(command);
    }
    
    public async Task<IEnumerable<int>> BulkCreateOrGetAsync(IEnumerable<Point> points, CancellationToken ct = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(ct);
        const string sql = """
                           INSERT INTO public.locations (point)
                           SELECT points.point FROM UNNEST(@Points) AS points
                           RETURNING id
                           """;
        var parameters = new
        {
            Points = points.ToArray()
        };
        var command = CreateCommand(sql, parameters, ct: ct);
        return await connection.QueryAsync<int>(command);
    }

    public async Task<IEnumerable<GridClusterWithResourceIds>> GetWindowedAndClusteredByGrid(GetWindowedAndClusteredByGridParameters parameters, CancellationToken ct = default)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = """
                           SELECT 
                               ST_Centroid(ST_Collect(l.point::geometry)) AS Center,
                               COUNT(DISTINCT l.id) AS Count,
                               array_agg(DISTINCT r.id) AS ResourceIds
                           FROM public.locations l
                           INNER JOIN public.resource_location rl ON rl.resource_id = l.id
                           INNER JOIN public.resource r ON r.id = rl.resource_id
                           WHERE l.point && ST_MakeEnvelope(@MinLon, @MinLat, @MaxLon, @MaxLat,4326)::geography
                               AND (@BranchPath IS NULL OR r.resource_branch <@ @BranchPath::ltree)
                           GROUP BY ST_SnapToGrid(l.point::geometry, @GridSize);
                           """;
        var command = CreateCommand(sql, parameters, ct: ct);
        return await connection.QueryAsync<GridClusterWithResourceIds>(command);
    }


    private CommandDefinition CreateCommand(
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CancellationToken ct = default)
    {
        return new CommandDefinition(
            sql,
            parameters,
            transaction,
            cancellationToken: ct
        );
    }
}