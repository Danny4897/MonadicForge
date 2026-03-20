using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC007 — Output di LLM usato direttamente senza validazione al boundary</summary>
public sealed class GC007_LlmOutputWithoutValidation : IAnalyzerRule
{
    public string RuleId => "GC007";
    public string Description => "Validate LLM output at the boundary with ValidatedResult.";
    public FindingSeverity Severity => FindingSeverity.Error;

    private static readonly HashSet<string> LlmOutputMethods =
    [
        "CompleteAsync", "GetChatMessageContentsAsync", "GenerateAsync",
        "CreateAsync", "CompleteChatAsync", "GetCompletionAsync"
    ];

    private static readonly HashSet<string> ValidationMethods =
    [
        "ValidatedResult", "ParseAs", "Validate", "Ensure", "Where", "Guard"
    ];

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC007_LlmOutputWithoutValidation rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var methodName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                _ => null
            };

            if (methodName is not null && LlmOutputMethods.Contains(methodName))
            {
                // Check that somewhere up the chain there's a validation
                var parent = node.Parent;
                bool validated = false;
                int depth = 0;
                while (parent is not null && depth < 10)
                {
                    if (parent is InvocationExpressionSyntax parentInv)
                    {
                        var parentMethod = parentInv.Expression switch
                        {
                            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                            _ => null
                        };
                        if (parentMethod is not null && ValidationMethods.Contains(parentMethod))
                        {
                            validated = true;
                            break;
                        }
                    }
                    parent = parent.Parent;
                    depth++;
                }

                if (!validated)
                {
                    var pos = tree.GetLineSpan(node.Span);
                    Findings.Add(new AnalysisFinding(
                        rule.RuleId,
                        rule.Severity,
                        rule.Description,
                        filePath,
                        pos.StartLinePosition.Line + 1,
                        pos.StartLinePosition.Character + 1,
                        "result.Bind(output => output.ValidatedResult<MyModel>())"
                    ));
                }
            }
            base.VisitInvocationExpression(node);
        }
    }
}
