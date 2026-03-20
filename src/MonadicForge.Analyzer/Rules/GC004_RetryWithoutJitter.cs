using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC004 — .WithRetry() / ThenWithRetry senza useJitter:true</summary>
public sealed class GC004_RetryWithoutJitter : IAnalyzerRule
{
    public string RuleId => "GC004";
    public string Description => "Always use useJitter: true to prevent thundering herd.";
    public FindingSeverity Severity => FindingSeverity.Warning;

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC004_RetryWithoutJitter rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var methodName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            };

            if (methodName is "WithRetry" or "ThenWithRetry")
            {
                bool hasJitter = node.ArgumentList.Arguments
                    .Any(arg =>
                        arg.NameColon?.Name.Identifier.Text == "useJitter" &&
                        arg.Expression is LiteralExpressionSyntax { Token.ValueText: "true" });

                if (!hasJitter)
                {
                    var pos = tree.GetLineSpan(node.Span);
                    Findings.Add(new AnalysisFinding(
                        rule.RuleId,
                        rule.Severity,
                        rule.Description,
                        filePath,
                        pos.StartLinePosition.Line + 1,
                        pos.StartLinePosition.Character + 1,
                        ".WithRetry(maxAttempts: 3, useJitter: true)"
                    ));
                }
            }
            base.VisitInvocationExpression(node);
        }
    }
}
