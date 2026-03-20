using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC010Tests
{
    private readonly GC010_SequenceOnLargeCollection _rule = new();

    [Fact]
    public void Detects_Sequence_On_Batch_Collection()
    {
        const string source = """
            class BatchProcessor
            {
                public Result<IEnumerable<Order>> Process(List<Result<Order>> allResults) =>
                    allResults.Sequence();
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "BatchProcessor.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC010", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Info, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_When_Using_Partition()
    {
        const string source = """
            class BatchProcessor
            {
                public (IEnumerable<Order> Ok, IEnumerable<Error> Err) Process(
                    List<Result<Order>> allResults) =>
                    allResults.Partition();
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "BatchProcessor.cs").ToList();

        Assert.Empty(findings);
    }
}
