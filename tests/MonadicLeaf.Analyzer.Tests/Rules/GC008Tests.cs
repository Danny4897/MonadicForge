using Microsoft.CodeAnalysis.CSharp;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Analyzer.Rules;

namespace MonadicLeaf.Analyzer.Tests.Rules;

public sealed class GC008Tests
{
    private readonly GC008_OverGrantedCapability _rule = new();

    [Fact]
    public void Detects_AgentCapability_All()
    {
        const string source = """
            class MyAgent
            {
                public AgentCapability RequiredCapabilities => AgentCapability.All;
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "MyAgent.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC008", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Warning, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_For_Specific_Capabilities()
    {
        const string source = """
            class MyAgent
            {
                public AgentCapability RequiredCapabilities =>
                    AgentCapability.ReadLocalFiles | AgentCapability.CallExternalApis;
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "MyAgent.cs").ToList();

        Assert.Empty(findings);
    }
}
