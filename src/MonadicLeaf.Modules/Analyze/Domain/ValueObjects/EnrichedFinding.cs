using MonadicLeaf.Analyzer.Core;

namespace MonadicLeaf.Modules.Analyze.Domain.ValueObjects;

/// <summary>
/// A Roslyn finding enriched (optionally) with LLM explanation and suggested fix.
/// </summary>
public sealed record EnrichedFinding(
    string RuleId,
    FindingSeverity Severity,
    string Description,
    string FilePath,
    int Line,
    int Column,
    string Suggestion,
    string? LlmExplanation,
    string? LlmSuggestedFix)
{
    public static EnrichedFinding FromRoslyn(AnalysisFinding finding) =>
        new(finding.RuleId, finding.Severity, finding.Description,
            finding.FilePath, finding.Line, finding.Column, finding.Suggestion,
            null, null);

    public EnrichedFinding WithLlmEnrichment(string? explanation, string? suggestedFix) =>
        this with { LlmExplanation = explanation, LlmSuggestedFix = suggestedFix };
}
