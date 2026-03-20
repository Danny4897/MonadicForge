namespace MonadicForge.Migrator.Core;

public sealed record MigrationResult(
    string FilePath,
    int ChangesApplied,
    IReadOnlyList<string> AppliedRules,
    string? NewSource
)
{
    public bool HasChanges => ChangesApplied > 0;
}
