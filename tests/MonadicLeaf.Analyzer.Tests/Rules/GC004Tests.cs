using Microsoft.CodeAnalysis.CSharp;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Analyzer.Rules;

namespace MonadicLeaf.Analyzer.Tests.Rules;

public sealed class GC004Tests
{
    private readonly GC004_RetryWithoutJitter _rule = new();

    [Fact]
    public void Detects_WithRetry_Without_UseJitter()
    {
        const string source = """
            class Agent
            {
                public Task<Result<Order>> Execute() =>
                    CallApi().WithRetry(maxAttempts: 3);
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Agent.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC004", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Warning, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_When_UseJitter_True()
    {
        const string source = """
            class Agent
            {
                public Task<Result<Order>> Execute() =>
                    CallApi().WithRetry(maxAttempts: 3, useJitter: true);
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Agent.cs").ToList();

        Assert.Empty(findings);
    }
}
