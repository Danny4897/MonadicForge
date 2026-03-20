using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC007Tests
{
    private readonly GC007_LlmOutputWithoutValidation _rule = new();

    [Fact]
    public void Detects_LlmOutput_Without_Validation()
    {
        const string source = """
            class Agent
            {
                public async Task<string> Run(string prompt)
                {
                    var result = await _chat.CompleteAsync(prompt);
                    return result.Content;
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Agent.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC007", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Error, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_When_Validated_With_Ensure()
    {
        const string source = """
            class Agent
            {
                public async Task<Result<string>> Run(string prompt)
                {
                    return (await Try.ExecuteAsync(() => _chat.CompleteAsync(prompt)))
                        .Ensure(r => !string.IsNullOrEmpty(r.Content), "Empty LLM output");
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Agent.cs").ToList();

        Assert.Empty(findings);
    }
}
