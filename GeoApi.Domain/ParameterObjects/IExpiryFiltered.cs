namespace GeoApi.Domain.ParameterObjects;

public interface IExpiryFiltered
{
    public bool IncludeExpired { get; set; }
}
