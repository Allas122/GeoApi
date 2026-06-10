using FluentMigrator;

namespace Migrations.Migrations;

public abstract class SqlMigration : Migration
{
    private readonly string _upPath;
    private readonly string _downPath;

    protected SqlMigration(string scriptFolder)
    {
        var assembly = GetType().Assembly;
        var assemblyName = assembly.GetName().Name; 
        var folder = char.IsDigit(scriptFolder[0]) ? "_" + scriptFolder : scriptFolder;
        _upPath = $"{assemblyName}.Scripts.{folder}.Up.sql";
        _downPath = $"{assemblyName}.Scripts.{folder}.Down.sql";
    }

    public override void Up()
    {
        Execute.Sql(ReadResource(_upPath));
    }

    public override void Down()
    {
        Execute.Sql(ReadResource(_downPath));
    }

    private string ReadResource(string resourceName)
    {
        var assembly = GetType().Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new Exception($"Resource '{resourceName}' not found. Available resources: {available}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}