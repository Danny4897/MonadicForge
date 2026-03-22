using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Analyzer.Core;

namespace MonadicLeaf.Analyzer.Rules;

/// <summary>
/// GC001 — try/catch annidato dentro una lambda passata a .Bind()
///
/// SemanticModel enhancement: when a SemanticModel is available, verifies that
/// the Bind/BindAsync call belongs to MonadicSharp (Result&lt;T&gt;, Option&lt;T&gt;, or their
/// extension method classes). This eliminates false positives from user-defined
/// Bind methods on non-monadic types.
/// </summary>
public sealed class GC001_TryCatchInsideBind : IAnalyzerRule
{
    public string RuleId => "GC001";
    public string Description => "try/catch inside Bind breaks the railway. Use Try.ExecuteAsync.";
    public FindingSeverity Severity => FindingSeverity.Error;

    public IEnumerable<AnalysisFinding> Analyze(
        SyntaxTree tree,
        string filePath,
        SemanticModel? semanticModel = null)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this, semanticModel);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(
        SyntaxTree tree,
        string filePath,
        GC001_TryCatchInsideBind rule,
        SemanticModel? semanticModel)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (IsBindCall(node) && IsMonadicSharpBind(node))
            {
                foreach (var arg in node.ArgumentList.Arguments)
                {
                    foreach (var tc in arg.DescendantNodes().OfType<TryStatementSyntax>())
                    {
                        var pos = tree.GetLineSpan(tc.Span);
                        Findings.Add(new AnalysisFinding(
                            rule.RuleId,
                            rule.Severity,
                            rule.Description,
                            filePath,
                            pos.StartLinePosition.Line + 1,
                            pos.StartLinePosition.Character + 1,
                            "return await Try.ExecuteAsync(() => yourOperation());"
                        ));
                    }
                }
            }
            base.VisitInvocationExpression(node);
        }

        private static bool IsBindCall(InvocationExpressionSyntax node) =>
            node.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Bind" or "BindAsync" };

        /// <summary>
        /// Returns true if the Bind call is from MonadicSharp (or symbol can't be resolved — keep finding).
        /// Returns false only when the symbol is confirmed to be from a non-MonadicSharp type.
        /// </summary>
        private bool IsMonadicSharpBind(InvocationExpressionSyntax node)
        {
            if (semanticModel is null)
                return true; // no model → syntactic check only, keep finding

            var symbolInfo = semanticModel.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (symbol is not IMethodSymbol method)
                return true; // can't resolve → keep finding (false negative is worse)

            // Check containing namespace or type for MonadicSharp markers
            var ns = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var typeName = method.ContainingType?.Name ?? string.Empty;

            // Extension methods on Result<T>/Option<T> live in MonadicSharp.Extensions
            // Instance methods live directly on Result<T>/Option<T>
            if (ns.StartsWith("MonadicSharp", StringComparison.Ordinal))
                return true;

            // Check the receiver type — if it contains Result or Option, it's likely monadic
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                var receiverName = receiverType?.Name ?? string.Empty;
                var receiverNs = receiverType?.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                if (receiverNs.StartsWith("MonadicSharp", StringComparison.Ordinal) ||
                    receiverName is "Result" or "Option")
                    return true;
            }

            // Symbol resolved to something outside MonadicSharp → not a false positive, skip
            return false;
        }
    }
}
