using MonadicLeaf.Migrator.Core;
using MonadicLeaf.Migrator.Rules;

namespace MonadicLeaf.Migrator.Tests.Rules;

public sealed class M003Tests
{
    private readonly MigrationEngine _engine = new([new M003_AddJitterToWithRetry()]);

    [Fact]
    public void Adds_UseJitter_True_To_WithRetry()
    {
        const string source = """
            var result = pipeline.WithRetry(maxAttempts: 3);
            """;

        var migrated = _engine.MigrateSource(source);

        Assert.True(migrated.IsSuccess);
        Assert.True(migrated.Value.HasChanges);
        var newSource = migrated.Value.NewSource ?? string.Empty;
        Assert.Contains("useJitter", newSource);
        Assert.Contains("true", newSource);
    }

    [Fact]
    public void Is_Idempotent_When_UseJitter_Already_Present()
    {
        const string source = """
            var result = pipeline.WithRetry(maxAttempts: 3, useJitter: true);
            """;

        var migrated = _engine.MigrateSource(source);

        Assert.True(migrated.IsSuccess);
        Assert.False(migrated.Value.HasChanges);
    }
}
