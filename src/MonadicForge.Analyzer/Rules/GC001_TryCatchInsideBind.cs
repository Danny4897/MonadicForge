using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC001 — try/catch annidato dentro una lambda passata a .Bind()</summary>
public sealed class GC001_TryCatchInsideBind : IAnalyzerRule
{
    public string RuleId => "GC001";
    public string Description => "try/catch inside Bind breaks the railway. Use Try.ExecuteAsync.";
    public FindingSeverity Severity => FindingSeverity.Error;

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC001_TryCatchInsideBind rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (IsBindCall(node))
            {
                // Look for try/catch inside any lambda argument of this Bind call
                foreach (var arg in node.ArgumentList.Arguments)
                {
                    var tryCatches = arg.DescendantNodes()
                        .OfType<TryStatementSyntax>();
                    foreach (var tc in tryCatches)
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
    }
}
