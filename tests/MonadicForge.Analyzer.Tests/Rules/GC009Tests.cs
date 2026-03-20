using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC009Tests
{
    private readonly GC009_MissingCircuitBreaker _rule = new();

    [Fact]
    public void Detects_ExternalAgent_Without_CircuitBreaker()
    {
        const string source = """
            class OrderService
            {
                private readonly IHttpResultClient _httpClient;
                public OrderService(IHttpResultClient httpClient)
                { _httpClient = httpClient; }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "OrderService.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC009", findings[0].RuleId);
    }

    [Fact]
    public void No_Finding_When_CircuitBreaker_Present()
    {
        const string source = """
            class OrderService
            {
                private readonly IHttpResultClient _httpClient;
                private readonly CircuitBreaker _breaker;
                public OrderService(IHttpResultClient httpClient, CircuitBreaker breaker)
                { _httpClient = httpClient; _breaker = breaker; }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "OrderService.cs").ToList();

        Assert.Empty(findings);
    }
}
