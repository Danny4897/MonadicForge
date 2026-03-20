using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC002Tests
{
    private readonly GC002_MapOnFallibleOperation _rule = new();

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
}
