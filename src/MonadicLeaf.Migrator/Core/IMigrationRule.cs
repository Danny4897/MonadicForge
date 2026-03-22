using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonadicSharp;

namespace MonadicLeaf.Migrator.Core;

public interface IMigrationRule
{
    string RuleId { get; }
    string Description { get; }

    /// <summary>Returns the modified syntax tree, or the original if no changes applied.</summary>
    Result<SyntaxNode> Apply(SyntaxNode root);
}
