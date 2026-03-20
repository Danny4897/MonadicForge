using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Migrator.Rules;
using MonadicSharp;

namespace MonadicForge.Migrator.Core;

public sealed class MigrationEngine
{
    private readonly IReadOnlyList<IMigrationRule> _rules;

    public MigrationEngine(IEnumerable<IMigrationRule>? rules = null)
    {
        _rules = rules?.ToList() ?? DefaultMigrationRules.All;
    }

    public Result<MigrationResult> MigrateFile(string filePath) =>
        Try.Execute(() => File.ReadAllText(filePath))
           .Bind(source => MigrateSource(source, filePath))
           .Bind(result =>
           {
               if (result.HasChanges && result.NewSource is not null)
                   File.WriteAllText(filePath, result.NewSource);
               return Result<MigrationResult>.Success(result);
           });

    public Result<MigrationResult> MigrateSource(string source, string filePath = "<inline>")
    {
        return Try.Execute(() =>
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetRoot();

            var appliedRules = new List<string>();
            int totalChanges = 0;
            SyntaxNode current = root;

            foreach (var rule in _rules)
            {
                var ruleResult = rule.Apply(current);
                ruleResult.Do(newRoot =>
                {
                    if (newRoot != current)
                    {
                        appliedRules.Add(rule.RuleId);
                        totalChanges++;
                        current = newRoot;
                    }
                });
            }

            string? newSource = totalChanges > 0 ? current.ToFullString() : null;
            return new MigrationResult(filePath, totalChanges, appliedRules, newSource);
        });
    }

    public Result<IReadOnlyList<MigrationResult>> MigratePath(string rootPath) =>
        Try.Execute(() =>
        {
            var csFiles = Directory
                .EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .ToList();

            var results = new List<MigrationResult>();
            foreach (var file in csFiles)
            {
                var result = MigrateFile(file);
                result.Do(r => results.Add(r));
            }

            return (IReadOnlyList<MigrationResult>)results;
        });
}
