using System.Data;
using Dapper;

namespace GeoApi.Infrastructure.Database.Parameters;

public sealed class ArrayParameter<T> : SqlMapper.ICustomQueryParameter
{
    private readonly T[] _values;

    public ArrayParameter(T[] values)
    {
        _values = values;
    }

    public void AddParameter(IDbCommand command, string name)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = _values;
        command.Parameters.Add(parameter);
    }
}
