using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MonadicSharp;

namespace MonadicLeaf.Analyzer.Core;

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
            var semanticModel = TryBuildSemanticModel(tree);

            var findings = _rules
                .SelectMany(rule => rule.Analyze(tree, filePath, semanticModel))
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

    /// <summary>
    /// Builds a SemanticModel by creating a CSharpCompilation that includes
    /// all assemblies loaded in the current AppDomain (which includes MonadicSharp
    /// since MonadicLeaf.Analyzer references it). Returns null on failure so
    /// rules can fall back to syntactic analysis gracefully.
    /// </summary>
    private static SemanticModel? TryBuildSemanticModel(SyntaxTree tree)
    {
        return Try.Execute(() =>
        {
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: "MonadicLeafAnalysis",
                syntaxTrees: [tree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));

            return compilation.GetSemanticModel(tree);
        })
        .GetValueOrDefault((SemanticModel?)null);
    }
}
