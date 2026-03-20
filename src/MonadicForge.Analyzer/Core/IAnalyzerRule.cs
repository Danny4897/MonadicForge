using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MonadicForge.Analyzer.Core;

public interface IAnalyzerRule
{
    string RuleId { get; }
    string Description { get; }
    FindingSeverity Severity { get; }

    IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath);
}
