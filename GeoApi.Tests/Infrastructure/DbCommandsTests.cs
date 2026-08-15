using GeoApi.Infrastructure.Database;

namespace GeoApi.Tests.Infrastructure;

public class DbCommandsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NormalizeTimeout_TreatsNonPositiveAsUnset(int configured)
    {
        Assert.Null(DbCommands.NormalizeTimeout(configured));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(600)]
    public void NormalizeTimeout_KeepsPositiveValues(int configured)
    {
        Assert.Equal(configured, DbCommands.NormalizeTimeout(configured));
    }

    [Fact]
    public void Create_AppliesTimeoutToCommandDefinition()
    {
        var command = DbCommands.Create("SELECT 1", null, null, 42, CancellationToken.None);

        Assert.Equal(42, command.CommandTimeout);
    }

    [Fact]
    public void Create_WithoutTimeout_LeavesCommandDefinitionDefault()
    {
        var command = DbCommands.Create("SELECT 1", null, null, null, CancellationToken.None);

        Assert.Null(command.CommandTimeout);
    }
}
