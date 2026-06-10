namespace GeoApi.Domain.Entities;

public interface IPaginatedById
{
    public int LastId { get; set; }
    public int Limit { get; set; }
}