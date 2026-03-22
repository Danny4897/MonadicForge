using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MonadicLeaf.Analyzer.Core;

public interface IAnalyzerRule
{
    string RuleId { get; }
    string Description { get; }
    FindingSeverity Severity { get; }

    /// <param name="tree">Parsed syntax tree of the file under analysis.</param>
    /// <param name="filePath">Source file path for reporting.</param>
    /// <param name="semanticModel">
    ///   Optional semantic model. When provided, rules can perform type-aware checks
    ///   to eliminate false positives. When null, rules fall back to syntactic analysis.
    /// </param>
    IEnumerable<AnalysisFinding> Analyze(
        SyntaxTree tree,
        string filePath,
        SemanticModel? semanticModel = null);
}
