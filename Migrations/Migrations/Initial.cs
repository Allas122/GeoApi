using FluentMigrator;

namespace Migrations.Migrations;

[Migration(20260531001)]
public class Initial : SqlMigration {
    public Initial() : base("001_Initial")
    {
    }
}