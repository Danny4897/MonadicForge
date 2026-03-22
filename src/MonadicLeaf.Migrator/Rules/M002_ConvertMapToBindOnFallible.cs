using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Migrator.Core;
using MonadicSharp;

namespace MonadicLeaf.Migrator.Rules;

/// <summary>
/// M002 — Converte .Map(x =&gt; riskyOp(x)) in .Bind(x =&gt; Try.Execute(() =&gt; riskyOp(x)))
/// per operazioni fallibili.
/// </summary>
public sealed class M002_ConvertMapToBindOnFallible : IMigrationRule
{
    public string RuleId => "M002";
    public string Description => "Convert .Map(fallible) to .Bind(x => Try.Execute(fallible))";

    private static readonly HashSet<string> FallibleMethodNames =
    [
        "Parse", "ParseExact", "Deserialize", "DeserializeObject",
        "ReadAllText", "ReadAllLines", "ReadAllBytes",
        "Open", "OpenRead", "OpenWrite", "Create", "Delete", "Move",
        "GetResponse", "Convert", "ToObject", "FromJson"
    ];

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
            // Visit children first (bottom-up)
            node = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;

            if (node.Expression is not MemberAccessExpressionSyntax
                { Name.Identifier.Text: "Map" } memberAccess)
                return node;

            if (node.ArgumentList.Arguments.Count != 1)
                return node;

            var arg = node.ArgumentList.Arguments[0].Expression;

            // Must be a lambda
            if (arg is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax))
                return node;

            // Body must contain a fallible call
            SyntaxNode? body = arg switch
            {
                SimpleLambdaExpressionSyntax s => (SyntaxNode?)s.ExpressionBody ?? s.Block,
                ParenthesizedLambdaExpressionSyntax p => (SyntaxNode?)p.ExpressionBody ?? p.Block,
                _ => null
            };

            if (body is null)
                return node;

            // We can only rewrite if there's an expression body (not a block body)
            ExpressionSyntax? exprBody = arg switch
            {
                SimpleLambdaExpressionSyntax s => s.ExpressionBody,
                ParenthesizedLambdaExpressionSyntax p => p.ExpressionBody,
                _ => null
            };

            bool isFallible = body.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Any(inv =>
                {
                    var name = inv.Expression switch
                    {
                        MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                        IdentifierNameSyntax id => id.Identifier.Text,
                        _ => null
                    };
                    return name is not null && FallibleMethodNames.Contains(name);
                });

            if (!isFallible || exprBody is null)
                return node;

            // Build: .Bind(x => Try.Execute(() => exprBody))
            // Preserve original lambda parameter
            ParameterSyntax? param = arg switch
            {
                SimpleLambdaExpressionSyntax s => s.Parameter,
                ParenthesizedLambdaExpressionSyntax p => p.ParameterList.Parameters.FirstOrDefault(),
                _ => null
            };

            var paramName = param?.Identifier.Text ?? "x";
            var executeCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Try"),
                    SyntaxFactory.IdentifierName("Execute")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.ParenthesizedLambdaExpression(
                                SyntaxFactory.ParameterList(),
                                exprBody)))));

            var newLambda = SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName)),
                executeCall);

            var newMember = memberAccess.WithName(SyntaxFactory.IdentifierName("Bind"));
            return node
                .WithExpression(newMember)
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(newLambda))));
        }
    }
}
