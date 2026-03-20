using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC005 — Validazioni pure dentro lo scope di WithRetry</summary>
public sealed class GC005_RetryWrapsValidation : IAnalyzerRule
{
    public string RuleId => "GC005";
    public string Description => "Move validation outside retry scope — terminal errors waste retry cycles.";
    public FindingSeverity Severity => FindingSeverity.Warning;

    private static readonly HashSet<string> ValidationMethodNames =
    [
        "Ensure", "Where", "Validate", "Guard", "Check", "Must", "Require"
    ];

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC005_RetryWrapsValidation rule)
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
                // Look inside the lambda arguments for validation calls
                var validationsInsideRetry = node.ArgumentList.Arguments
                    .SelectMany(arg => arg.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    .Where(inv =>
                    {
                        var name = inv.Expression switch
                        {
                            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                            IdentifierNameSyntax id => id.Identifier.Text,
                            _ => null
                        };
                        return name is not null && ValidationMethodNames.Contains(name);
                    });

                foreach (var validation in validationsInsideRetry)
                {
                    var pos = tree.GetLineSpan(node.Span);
                    Findings.Add(new AnalysisFinding(
                        rule.RuleId,
                        rule.Severity,
                        rule.Description,
                        filePath,
                        pos.StartLinePosition.Line + 1,
                        pos.StartLinePosition.Character + 1,
                        "Extract .Ensure()/Where() before WithRetry scope: result.Ensure(...).WithRetry(...);"
                    ));
                    break; // one finding per WithRetry
                }
            }
            base.VisitInvocationExpression(node);
        }
    }
}
