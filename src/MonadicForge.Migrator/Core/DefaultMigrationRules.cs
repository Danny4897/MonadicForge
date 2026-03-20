using MonadicForge.Migrator.Rules;

namespace MonadicForge.Migrator.Core;

public static class DefaultMigrationRules
{
    public static IReadOnlyList<IMigrationRule> All { get; } =
    [
        new M001_ConvertTryCatchToBind(),
        new M002_ConvertMapToBindOnFallible(),
        new M003_AddJitterToWithRetry(),
        new M004_WrapNullReturnWithOption(),
    ];
}
