using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>
/// GC003 — Operazioni async costose (DB/HTTP) prima di validazioni pure nella chain
///
/// Syntactic analysis is sufficient here: method names like FindAsync, GetAsync are
/// strong enough identifiers for expensive I/O. Adding SemanticModel would add
/// complexity with minimal false-positive reduction since the method name heuristics
/// already have high precision. Potential future improvement: use SemanticModel to
/// verify the receiver implements IRepository or IHttpClient.
/// </summary>
public sealed class GC003_ExpensiveBeforeValidation : IAnalyzerRule
{
    public string RuleId => "GC003";
    public string Description => "Cheap validation should gate expensive operations.";
    public FindingSeverity Severity => FindingSeverity.Warning;

    private static readonly HashSet<string> ExpensiveMethodNames =
    [
        "GetAsync", "PostAsync", "PutAsync", "DeleteAsync", "PatchAsync",
        "QueryAsync", "FindAsync", "FirstOrDefaultAsync", "ToListAsync",
        "SaveChangesAsync", "ExecuteAsync", "SendAsync"
    ];

    private static readonly HashSet<string> ValidationMethodNames =
    [
        "Where", "Ensure", "Validate", "Check", "Guard", "Require", "Must"
    ];

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath, SemanticModel? semanticModel = null)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC003_ExpensiveBeforeValidation rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // When we encounter a validation call, walk its receiver chain to find expensive calls before it
            if (IsValidation(node))
            {
                SyntaxNode? receiver = GetReceiver(node);
                while (receiver is not null)
                {
                    // Strip away await and parentheses
                    if (receiver is AwaitExpressionSyntax awaitExpr)
                        receiver = awaitExpr.Expression;
                    if (receiver is ParenthesizedExpressionSyntax paren)
                        receiver = paren.Expression;

                    if (receiver is InvocationExpressionSyntax inv)
                    {
                        if (IsExpensiveAsync(inv))
                        {
                            var pos = tree.GetLineSpan(inv.Span);
                            Findings.Add(new AnalysisFinding(
                                rule.RuleId,
                                rule.Severity,
                                rule.Description,
                                filePath,
                                pos.StartLinePosition.Line + 1,
                                pos.StartLinePosition.Character + 1,
                                "Reorder: call .Where(pureValidation) before expensive async operation."
                            ));
                            break;
                        }
                        receiver = GetReceiver(inv);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            base.VisitInvocationExpression(node);
        }

        private static SyntaxNode? GetReceiver(InvocationExpressionSyntax inv) =>
            inv.Expression is MemberAccessExpressionSyntax m ? m.Expression : null;

        private static bool IsExpensiveAsync(InvocationExpressionSyntax inv)
        {
            var name = inv.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                _ => null
            };
            return name is not null && ExpensiveMethodNames.Contains(name);
        }

        private static bool IsValidation(InvocationExpressionSyntax inv)
        {
            var name = inv.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                _ => null
            };
            return name is not null && ValidationMethodNames.Contains(name);
        }
    }
}
