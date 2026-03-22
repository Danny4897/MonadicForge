using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Migrator.Core;
using MonadicSharp;

namespace MonadicLeaf.Migrator.Rules;

/// <summary>
/// M001 — Converte pattern try/catch-to-Result in Try.ExecuteAsync
///
/// Input:
///   try { return Result&lt;T&gt;.Success(await op()); }
///   catch (Exception ex) { return Result&lt;T&gt;.Failure(Error.FromException(ex)); }
///
/// Output:
///   return await Try.ExecuteAsync(() =&gt; op());
/// </summary>
public sealed class M001_ConvertTryCatchToBind : IMigrationRule
{
    public string RuleId => "M001";
    public string Description => "Convert try/catch-to-Result pattern to Try.ExecuteAsync";

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
        public override SyntaxNode? VisitTryStatement(TryStatementSyntax node)
        {
            // Pattern: single try block with Result.Success(await expr) return
            // and single catch returning Result.Failure(Error.FromException(ex))
            if (node.Catches.Count != 1 || node.Finally is not null)
                return base.VisitTryStatement(node);

            var tryBlock = node.Block;
            var catchBlock = node.Catches[0];

            // Try block must have exactly: return Result<T>.Success(await someExpr());
            var tryReturns = tryBlock.Statements.OfType<ReturnStatementSyntax>().ToList();
            if (tryBlock.Statements.Count != 1 || tryReturns.Count != 1)
                return base.VisitTryStatement(node);

            var tryReturn = tryReturns[0];
            if (!IsResultSuccessWithAwait(tryReturn.Expression, out var innerExpression))
                return base.VisitTryStatement(node);

            // Catch block must have: return Result<T>.Failure(Error.FromException(ex))
            var catchReturns = catchBlock.Block.Statements.OfType<ReturnStatementSyntax>().ToList();
            if (catchBlock.Block.Statements.Count != 1 || catchReturns.Count != 1)
                return base.VisitTryStatement(node);

            if (!IsResultFailureFromException(catchReturns[0].Expression))
                return base.VisitTryStatement(node);

            // Build: return await Try.ExecuteAsync(() => innerExpression);
            var replacement = SyntaxFactory.ReturnStatement(
                SyntaxFactory.AwaitExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Try"),
                            SyntaxFactory.IdentifierName("ExecuteAsync")),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.ParenthesizedLambdaExpression(
                                        SyntaxFactory.ParameterList(),
                                        innerExpression)))))));

            return replacement
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        private static bool IsResultSuccessWithAwait(ExpressionSyntax? expr, out ExpressionSyntax? inner)
        {
            inner = null;
            if (expr is InvocationExpressionSyntax inv &&
                inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Success" } &&
                inv.ArgumentList.Arguments.Count == 1)
            {
                var arg = inv.ArgumentList.Arguments[0].Expression;
                if (arg is AwaitExpressionSyntax awaitExpr)
                {
                    inner = awaitExpr.Expression;
                    return true;
                }
            }
            return false;
        }

        private static bool IsResultFailureFromException(ExpressionSyntax? expr)
        {
            if (expr is InvocationExpressionSyntax inv &&
                inv.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Failure" } &&
                inv.ArgumentList.Arguments.Count == 1)
            {
                var arg = inv.ArgumentList.Arguments[0].Expression;
                return arg is InvocationExpressionSyntax innerInv &&
                       innerInv.Expression is MemberAccessExpressionSyntax
                       {
                           Name.Identifier.Text: "FromException"
                       };
            }
            return false;
        }
    }
}
