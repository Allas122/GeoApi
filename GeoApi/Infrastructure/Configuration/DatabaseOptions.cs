namespace GeoApi.Infrastructure.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "DatabaseSettings";
    
    public required string ConnectionString {get;set;}
    public bool Logging {get;set;}
    public int CommandTimeout {get;set;}
}