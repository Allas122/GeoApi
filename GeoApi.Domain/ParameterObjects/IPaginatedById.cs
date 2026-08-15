namespace GeoApi.Domain.ParameterObjects;

public interface IPaginatedById
{
    public int LastId { get; set; }
    public int Limit { get; set; }
}