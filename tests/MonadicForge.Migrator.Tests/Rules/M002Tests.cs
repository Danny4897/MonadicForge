using MonadicForge.Migrator.Core;
using MonadicForge.Migrator.Rules;

namespace MonadicForge.Migrator.Tests.Rules;

public sealed class M002Tests
{
    private readonly MigrationEngine _engine = new([new M002_ConvertMapToBindOnFallible()]);

    [Fact]
    public void Converts_Map_With_Parse_To_Bind_With_TryExecute()
    {
        const string source = """
            var result = input.Map(s => int.Parse(s));
            """;

        var migrated = _engine.MigrateSource(source);

        Assert.True(migrated.IsSuccess);
        Assert.True(migrated.Value.HasChanges);
        Assert.Contains("Bind", migrated.Value.NewSource ?? string.Empty);
        Assert.Contains("Try.Execute", migrated.Value.NewSource ?? string.Empty);
    }

    [Fact]
    public void Does_Not_Modify_Pure_Map()
    {
        const string source = """
            var result = input.Map(s => s.ToUpperInvariant());
            """;

        var migrated = _engine.MigrateSource(source);

        Assert.True(migrated.IsSuccess);
        Assert.False(migrated.Value.HasChanges);
    }
}
