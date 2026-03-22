namespace MonadicLeaf.Analyzer.Core;

public sealed record AnalysisFinding(
    string RuleId,
    FindingSeverity Severity,
    string Description,
    string FilePath,
    int Line,
    int Column,
    string Suggestion
)
{
    public override string ToString() =>
        $"[{Severity,-7}] {RuleId}  {System.IO.Path.GetFileName(FilePath)}:{Line}  {Description}";
}
