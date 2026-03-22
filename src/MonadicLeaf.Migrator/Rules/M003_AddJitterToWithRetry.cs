using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Migrator.Core;
using MonadicSharp;

namespace MonadicLeaf.Migrator.Rules;

/// <summary>
/// M003 — Aggiunge useJitter: true alle chiamate .WithRetry() che non ce l'hanno.
///
/// Input:  .WithRetry(maxAttempts: 3)
/// Output: .WithRetry(maxAttempts: 3, useJitter: true)
/// </summary>
public sealed class M003_AddJitterToWithRetry : IMigrationRule
{
    public string RuleId => "M003";
    public string Description => "Add useJitter: true to WithRetry calls";

    public Result<SyntaxNode> Apply(SyntaxNode root)
    {
        return Try.Execute(() =>
        {
            var rewriter = new Rewriter();
            return rewriter.Visit(root);
        });
    }

    private sealed class Rewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            node = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;

            var methodName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            };

            if (methodName is not ("WithRetry" or "ThenWithRetry"))
                return node;

            bool alreadyHasJitter = node.ArgumentList.Arguments
                .Any(arg => arg.NameColon?.Name.Identifier.Text == "useJitter");

            if (alreadyHasJitter)
                return node;

            // Append useJitter: true
            var jitterArg = SyntaxFactory.Argument(
                SyntaxFactory.NameColon(SyntaxFactory.IdentifierName("useJitter")),
                SyntaxFactory.Token(SyntaxKind.None),
                SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

            var newArgList = node.ArgumentList.AddArguments(jitterArg);
            return node.WithArgumentList(newArgList);
        }
    }
}
