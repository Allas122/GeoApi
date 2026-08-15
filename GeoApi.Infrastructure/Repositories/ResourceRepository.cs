using System.Data;
using System.Data.Common;
using Dapper;
using GeoApi.Domain.Entities;
using GeoApi.Domain.ParameterObjects.Resource;
using GeoApi.Domain.Repositories;
using GeoApi.Infrastructure.Configuration;
using GeoApi.Infrastructure.Database;
using GeoApi.Infrastructure.Database.Abstractions;
using GeoApi.Infrastructure.Database.DataTypes;
using GeoApi.Infrastructure.Database.Parameters;
using GeoApi.Infrastructure.Mappers;
using Microsoft.Extensions.Options;

using Microsoft.Extensions.Logging;

namespace GeoApi.Infrastructure.Repositories;

public class ResourceRepository : IResourceRepository
{
    private readonly IDbSession _dbSession;
    private readonly ILogger<ResourceRepository> _logger;
    private readonly int? _commandTimeout;

    public ResourceRepository(
        IDbSession session,
        ILogger<ResourceRepository> logger,
        IOptions<DatabaseOptions> options)
    {
        _dbSession = session;
        _logger = logger;
        _commandTimeout = DbCommands.NormalizeTimeout(options.Value.CommandTimeout);
    }

    private const string ResourceColumns = """
                                          SELECT r.id                    AS "Id",
                                                 r.resource_branch::text AS "ResourceBranch",
                                                 r.created_at            AS "CreatedAt",
                                                 r.updated_at            AS "UpdatedAt",
                                                 r.expires_in            AS "ExpiresIn"
                                          FROM public.resources r
                                          """;

    private const string NotExpiredPredicate = SqlPredicates.NotExpired;

    public async Task<ResourceEntity?> GetByIdAsync(GetResourceByIdParameters parameters, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            WHERE r.id = @Id
                              AND {NotExpiredPredicate}
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryFirstOrDefaultAsync<ResourceEntity>(command);
    }

    public async Task<IEnumerable<ResourceEntity>> GetByIdsAsync(GetResourcesByIdsParameters parameters, CancellationToken ct = default)
    {
        int[] resourceIds = parameters.Ids.ToArray();
        if (resourceIds.Length == 0)
        {
            return [];
        }

        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            WHERE r.id = ANY(@Ids)
                              AND {NotExpiredPredicate}
                            ORDER BY r.id
                            """;
        var sqlParameters = new
        {
            Ids = new ArrayParameter<int>(resourceIds),
            parameters.IncludeExpired
        };
        var command = CreateCommand(sql, sqlParameters, lease.Transaction, ct);
        return await lease.QueryAsync<ResourceEntity>(command);
    }

    public async Task<IEnumerable<ResourceEntity>> GetPageAsync(
        GetResourcesPageParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            WHERE r.id > @LastId
                              AND {NotExpiredPredicate}
                            ORDER BY r.id
                            LIMIT @Limit
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<ResourceEntity>(command);
    }

    public async Task<IEnumerable<ResourceEntity>> GetSubtreeAsync(
        GetResourceSubtreeParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            WHERE r.resource_branch <@ @BranchPath::ltree
                              AND (@MaxDepth::int IS NULL
                                   OR nlevel(r.resource_branch) - nlevel(@BranchPath::ltree) <= @MaxDepth)
                              AND (@IncludeSelf OR r.resource_branch <> @BranchPath::ltree)
                              AND r.id > @LastId
                              AND {NotExpiredPredicate}
                            ORDER BY r.id
                            LIMIT @Limit
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<ResourceEntity>(command);
    }

    public async Task<IEnumerable<ResourceEntity>> GetAncestorsAsync(
        GetResourceAncestorsParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            WHERE r.resource_branch @> @BranchPath::ltree
                              AND r.id > @LastId
                              AND {NotExpiredPredicate}
                            ORDER BY r.id
                            LIMIT @Limit
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<ResourceEntity>(command);
    }

    public async Task<IEnumerable<ResourceEntity>> GetByBranchPatternAsync(
        GetResourcesByBranchPatternParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            WHERE r.resource_branch ~ @Pattern::lquery
                              AND r.id > @LastId
                              AND {NotExpiredPredicate}
                            ORDER BY r.id
                            LIMIT @Limit
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<ResourceEntity>(command);
    }

    public async Task<IEnumerable<LocationEntity>> GetLocationsAsync(
        GetResourceLocationsByIdParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            SELECT l.id                    AS "Id",
                                   ST_X(l.point::geometry) AS "Longitude",
                                   ST_Y(l.point::geometry) AS "Latitude"
                            FROM public.locations l
                            INNER JOIN public.resource_location rl ON rl.location_id = l.id
                            INNER JOIN public.resources r          ON r.id = rl.resource_id
                            WHERE rl.resource_id = @ResourceId
                              AND l.id > @LastId
                              AND {NotExpiredPredicate}
                            ORDER BY l.id
                            LIMIT @Limit
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return (await lease.QueryAsync<LocationRow>(command)).MapToLocations();
    }

    public async Task<IEnumerable<ResourceEntity>> GetByLocationIdAsync(
        GetResourcesByLocationIdParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = $"""
                            {ResourceColumns}
                            INNER JOIN public.resource_location rl ON rl.resource_id = r.id
                            WHERE rl.location_id = @LocationId
                              AND r.id > @LastId
                              AND {NotExpiredPredicate}
                            ORDER BY r.id
                            LIMIT @Limit
                            """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<ResourceEntity>(command);
    }

    public async Task<int> CreateAsync(ResourceEntity resource, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           INSERT INTO public.resources (resource_branch, expires_in)
                           VALUES (@ResourceBranch::ltree, @ExpiresIn)
                           RETURNING id
                           """;
        var parameters = new
        {
            resource.ResourceBranch,
            resource.ExpiresIn
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QuerySingleAsync<int>(command);
    }

    public async Task<IReadOnlyList<int>> BulkCreateAsync(
        IReadOnlyList<ResourceEntity> resources,
        CancellationToken ct = default)
    {
        if (resources.Count == 0)
        {
            return [];
        }

        _logger.LogDebug("Bulk create for {ResourceCount} resources", resources.Count);

        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           WITH input AS MATERIALIZED (
                               SELECT nextval(pg_get_serial_sequence('public.resources', 'id')) AS id,
                                      t.branch,
                                      t.expires_in,
                                      t.ord
                               FROM UNNEST(@Branches, @ExpiresIn) WITH ORDINALITY AS t(branch, expires_in, ord)
                           ),
                           inserted AS (
                               INSERT INTO public.resources (id, resource_branch, expires_in)
                               SELECT id, branch::ltree, expires_in FROM input
                           )
                           SELECT id FROM input ORDER BY ord
                           """;

        var parameters = new
        {
            Branches = new ArrayParameter<string>(resources.Select(r => r.ResourceBranch).ToArray()),
            ExpiresIn = new ArrayParameter<TimeSpan>(resources.Select(r => r.ExpiresIn).ToArray())
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return (await lease.QueryAsync<int>(command)).ToArray();
    }

    public async Task<ResourceEntity?> UpdateAsync(
        UpdateResourceParameters parameters,
        CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           UPDATE public.resources r
                           SET resource_branch = @ResourceBranch::ltree,
                               expires_in      = @ExpiresIn
                           WHERE r.id = @Id
                           RETURNING r.id                    AS "Id",
                                     r.resource_branch::text AS "ResourceBranch",
                                     r.created_at            AS "CreatedAt",
                                     r.updated_at            AS "UpdatedAt",
                                     r.expires_in            AS "ExpiresIn"
                           """;
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryFirstOrDefaultAsync<ResourceEntity>(command);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);

        var parameters = new
        {
            Id = id
        };

        const string unlinkSql = """
                                 DELETE FROM public.resource_location
                                 WHERE resource_id = @Id
                                 """;
        await lease.ExecuteAsync(CreateCommand(unlinkSql, parameters, lease.Transaction, ct));

        const string deleteSql = """
                                 DELETE FROM public.resources
                                 WHERE id = @Id
                                 """;
        int affected = await lease.ExecuteAsync(CreateCommand(deleteSql, parameters, lease.Transaction, ct));

        return affected > 0;
    }

    public async Task<bool> LinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           INSERT INTO public.resource_location (resource_id, location_id)
                           VALUES (@ResourceId, @LocationId)
                           ON CONFLICT DO NOTHING
                           """;
        var parameters = new
        {
            ResourceId = resourceId,
            LocationId = locationId
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.ExecuteAsync(command) > 0;
    }

    public async Task<IEnumerable<int>> BulkLinkLocationsAsync(
        int resourceId,
        IEnumerable<int> locationIds,
        CancellationToken ct = default)
    {
        int[] ids = locationIds.ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        _logger.LogDebug("Linking resource {ResourceId} to {LocationCount} locations", resourceId, ids.Length);

        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           INSERT INTO public.resource_location (resource_id, location_id)
                           SELECT @ResourceId, location_id
                           FROM UNNEST(@LocationIds) AS location_id
                           ON CONFLICT DO NOTHING
                           RETURNING location_id
                           """;
        var parameters = new
        {
            ResourceId = resourceId,
            LocationIds = new ArrayParameter<int>(ids)
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<int>(command);
    }

    public async Task<int> BulkLinkPairsAsync(
        IReadOnlyList<int> resourceIds,
        IReadOnlyList<int> locationIds,
        CancellationToken ct = default)
    {
        if (resourceIds.Count == 0)
        {
            return 0;
        }

        if (resourceIds.Count != locationIds.Count)
        {
            throw new ArgumentException("Resource and location id lists must have the same length.", nameof(locationIds));
        }

        _logger.LogDebug("Bulk linking {PairCount} resource-location pairs", resourceIds.Count);

        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           INSERT INTO public.resource_location (resource_id, location_id)
                           SELECT DISTINCT resource_id, location_id
                           FROM UNNEST(@ResourceIds, @LocationIds) AS t(resource_id, location_id)
                           ON CONFLICT DO NOTHING
                           """;

        var parameters = new
        {
            ResourceIds = new ArrayParameter<int>(resourceIds.ToArray()),
            LocationIds = new ArrayParameter<int>(locationIds.ToArray())
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.ExecuteAsync(command);
    }

    public async Task<bool> UnlinkLocationAsync(int resourceId, int locationId, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           DELETE FROM public.resource_location
                           WHERE resource_id = @ResourceId
                             AND location_id = @LocationId
                           """;
        var parameters = new
        {
            ResourceId = resourceId,
            LocationId = locationId
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.ExecuteAsync(command) > 0;
    }

    public async Task<IEnumerable<int>> BulkUnlinkLocationsAsync(
        int resourceId,
        IEnumerable<int> locationIds,
        CancellationToken ct = default)
    {
        int[] ids = locationIds.ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        _logger.LogDebug("Unlinking resource {ResourceId} from {LocationCount} locations", resourceId, ids.Length);

        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           DELETE FROM public.resource_location
                           WHERE resource_id = @ResourceId
                             AND location_id = ANY(@LocationIds)
                           RETURNING location_id
                           """;
        var parameters = new
        {
            ResourceId = resourceId,
            LocationIds = new ArrayParameter<int>(ids)
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.QueryAsync<int>(command);
    }

    public async Task<int> UnlinkAllLocationsAsync(int resourceId, CancellationToken ct = default)
    {
        await using var lease = await _dbSession.LeaseAsync(ct);
        const string sql = """
                           DELETE FROM public.resource_location
                           WHERE resource_id = @ResourceId
                           """;
        var parameters = new
        {
            ResourceId = resourceId
        };
        var command = CreateCommand(sql, parameters, lease.Transaction, ct);
        return await lease.ExecuteAsync(command);
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
