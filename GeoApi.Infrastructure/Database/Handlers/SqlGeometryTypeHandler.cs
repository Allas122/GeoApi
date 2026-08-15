using System.Data;
using Dapper;
using NetTopologySuite.Geometries;

namespace GeoApi.Infrastructure.Database.Handlers;

public class SqlGeometryTypeHandler<T> : SqlMapper.TypeHandler<T> where T : Geometry
{
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = value ?? (object)DBNull.Value;
    }

    public override T? Parse(object value)
    {
        if (value is null or DBNull)
        {
            return null;
        }

        if (value is T geometry)
        {
            return geometry;
        }

        throw new InvalidCastException(
            $"Expected {typeof(T).Name} from the database but received {value.GetType().Name}.");
    }
}
