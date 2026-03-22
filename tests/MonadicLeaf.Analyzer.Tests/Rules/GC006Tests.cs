using Microsoft.CodeAnalysis.CSharp;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Analyzer.Rules;

namespace MonadicLeaf.Analyzer.Tests.Rules;

public sealed class GC006Tests
{
    private readonly GC006_LlmCallWithoutCache _rule = new();

    [Fact]
    public void Detects_LlmField_Without_CachingWrapper()
    {
        const string source = """
            class SummaryAgent
            {
                private readonly IChatClient _chat;
                public SummaryAgent(IChatClient chat) { _chat = chat; }
                public Task<Result<string>> Run(string text) =>
                    Try.ExecuteAsync(() => _chat.CompleteAsync(text));
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "SummaryAgent.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC006", findings[0].RuleId);
    }

    [Fact]
    public void No_Finding_When_CachingWrapper_Present()
    {
        const string source = """
            class SummaryAgent
            {
                private readonly IChatClient _chat;
                private readonly ICacheService _cache;
                public SummaryAgent(IChatClient chat, ICacheService cache)
                { _chat = chat; _cache = cache; }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "SummaryAgent.cs").ToList();

        Assert.Empty(findings);
    }
}
