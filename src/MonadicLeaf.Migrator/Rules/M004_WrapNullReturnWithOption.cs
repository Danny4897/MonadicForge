using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Migrator.Core;
using MonadicSharp;

namespace MonadicLeaf.Migrator.Rules;

/// <summary>
/// M004 — Converte "return null;" in metodi nullable in "return Option&lt;T&gt;.None;"
///
/// Input:  return null;   (in metodo che ritorna T?)
/// Output: return Option&lt;T&gt;.None;
/// </summary>
public sealed class M004_WrapNullReturnWithOption : IMigrationRule
{
    public string RuleId => "M004";
    public string Description => "Wrap null returns with Option<T>.None";

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
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Only process methods returning Option<T> or T?
            var returnType = node.ReturnType.ToString();
            if (!returnType.StartsWith("Option<") && !returnType.EndsWith("?"))
                return base.VisitMethodDeclaration(node);

            // Extract type parameter T from Option<T> or nullable T?
            string typeParam = returnType.StartsWith("Option<")
                ? returnType[7..^1]  // Option<T> → T
                : returnType[..^1];  // T? → T

            var rewrittenNode = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;

            // Replace all "return null;" within this method body
            if (rewrittenNode.Body is null)
                return rewrittenNode;

            var newBody = rewrittenNode.Body.ReplaceNodes(
                rewrittenNode.Body.DescendantNodes().OfType<ReturnStatementSyntax>()
                    .Where(r => r.Expression is LiteralExpressionSyntax
                    {
                        Token.RawKind: (int)SyntaxKind.NullKeyword
                    }),
                (original, _) =>
                {
                    // return Option<T>.None;
                    var noneExpr = SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("Option"),
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(typeParam)))),
                        SyntaxFactory.IdentifierName("None"));

                    return original.WithExpression(noneExpr);
                });

            return rewrittenNode.WithBody(newBody);
        }
    }
}
