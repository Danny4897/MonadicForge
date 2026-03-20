using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC010 — .Sequence() su IEnumerable senza Partition() per batch processing</summary>
public sealed class GC010_SequenceOnLargeCollection : IAnalyzerRule
{
    public string RuleId => "GC010";
    public string Description => "Use Partition() for batch processing — Sequence fails on first error.";
    public FindingSeverity Severity => FindingSeverity.Info;

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC010_SequenceOnLargeCollection rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Sequence" })
            {
                // Check if the receiver is a collection variable (not a single element)
                // Heuristic: Sequence() called on a variable whose name suggests collection
                var receiver = (node.Expression as MemberAccessExpressionSyntax)?.Expression;
                var receiverText = receiver?.ToString() ?? string.Empty;

                bool likelyLargeCollection =
                    receiverText.Contains("List") ||
                    receiverText.Contains("Array") ||
                    receiverText.Contains("Items") ||
                    receiverText.Contains("All") ||
                    receiverText.Contains("Results") ||
                    receiverText.Contains("Enumerable") ||
                    receiverText.Contains("Many") ||
                    receiverText.Contains("Batch");

                if (likelyLargeCollection)
                {
                    var pos = tree.GetLineSpan(node.Span);
                    Findings.Add(new AnalysisFinding(
                        rule.RuleId,
                        rule.Severity,
                        rule.Description,
                        filePath,
                        pos.StartLinePosition.Line + 1,
                        pos.StartLinePosition.Character + 1,
                        "var (successes, failures) = results.Partition();"
                    ));
                }
            }
            base.VisitInvocationExpression(node);
        }
    }
}
