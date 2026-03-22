using Microsoft.CodeAnalysis.CSharp;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Analyzer.Rules;

namespace MonadicLeaf.Analyzer.Tests.Rules;

public sealed class GC002Tests
{
    private readonly GC002_MapOnFallibleOperation _rule = new();

    // ── Syntactic analysis (no SemanticModel) ─────────────────────────────────

    [Fact]
    public void Detects_Map_With_Parse_Call()
    {
        const string source = """
            class Parser
            {
                public Result<int> Parse(Result<string> input) =>
                    input.Map(s => int.Parse(s));
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Parser.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC002", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Error, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_For_Pure_Map()
    {
        const string source = """
            class Transformer
            {
                public Result<string> ToUpper(Result<string> input) =>
                    input.Map(s => s.ToUpperInvariant());
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Transformer.cs").ToList();

        Assert.Empty(findings);
    }

    // ── SemanticModel analysis ─────────────────────────────────────────────────

    [Fact]
    public void No_False_Positive_For_UserDefined_Convert_With_SemanticModel()
    {
        // "Convert" is in the fallible name list, but this one is a pure user-defined method.
        // With SemanticModel, it resolves to user namespace and is not in StrictFallibleMethodNames
        // (which requires dangerous names like Parse, Deserialize, ReadAllText).
        const string source = """
            using MonadicSharp;
            using System;
            class MyUtils
            {
                public static int Convert(string s) => s?.Length ?? 0;
            }
            class Service
            {
                public static Result<int> GetLength(Result<string> r) =>
                    r.Map(s => MyUtils.Convert(s));
            }
            """;

        var engine = new AnalysisEngine([_rule]);
        var result = engine.AnalyzeSource(source);

        Assert.True(result.IsSuccess);
        // SemanticModel confirms MyUtils.Convert is in user namespace and not in StrictFallibleMethodNames
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detects_Async_Method_Inside_Map_With_SemanticModel()
    {
        // Async method inside Map always produces Result<Task<T>> — definitely wrong.
        // SemanticModel detects the Task return type.
        const string source = """
            using MonadicSharp;
            using System;
            using System.Threading.Tasks;
            class DataService
            {
                public static async Task<string> ReadAsync(string path) =>
                    await System.IO.File.ReadAllTextAsync(path);
            }
            class Processor
            {
                public static Result<Task<string>> Process(Result<string> r) =>
                    r.Map(path => DataService.ReadAsync(path));
            }
            """;

        var engine = new AnalysisEngine([_rule]);
        var result = engine.AnalyzeSource(source);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, f => f.RuleId == "GC002");
    }
}
