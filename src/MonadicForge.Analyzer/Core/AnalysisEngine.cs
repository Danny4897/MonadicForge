using Microsoft.CodeAnalysis.CSharp;
using MonadicSharp;

namespace MonadicForge.Analyzer.Core;

public sealed class AnalysisEngine
{
    private readonly IReadOnlyList<IAnalyzerRule> _rules;

    public AnalysisEngine(IEnumerable<IAnalyzerRule>? rules = null)
    {
        _rules = rules?.ToList() ?? DefaultRules.All;
    }

    public Result<IReadOnlyList<AnalysisFinding>> AnalyzeFile(string filePath) =>
        Try.Execute(() => File.ReadAllText(filePath))
           .Bind(source => AnalyzeSource(source, filePath));

    public Result<IReadOnlyList<AnalysisFinding>> AnalyzeSource(string source, string filePath = "<inline>")
    {
        return Try.Execute(() =>
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var findings = _rules
                .SelectMany(rule => rule.Analyze(tree, filePath))
                .OrderBy(f => f.Line)
                .ThenBy(f => f.RuleId)
                .ToList();
            return (IReadOnlyList<AnalysisFinding>)findings;
        });
    }

    public Result<IReadOnlyList<AnalysisFinding>> AnalyzePath(string rootPath) =>
        Try.Execute(() =>
        {
            var csFiles = Directory
                .EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .ToList();

            var allFindings = new List<AnalysisFinding>();
            foreach (var file in csFiles)
            {
                var result = AnalyzeFile(file);
                result.Do(findings => allFindings.AddRange(findings));
            }

            return (IReadOnlyList<AnalysisFinding>)allFindings
                .OrderBy(f => f.FilePath)
                .ThenBy(f => f.Line)
                .ToList();
        });
}
