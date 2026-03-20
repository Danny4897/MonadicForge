using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC001Tests
{
    private readonly GC001_TryCatchInsideBind _rule = new();

    [Fact]
    public void Detects_TryCatch_Inside_Bind_Lambda()
    {
        const string source = """
            using MonadicSharp;
            class OrderService
            {
                public Result<Order> Process(int id) =>
                    GetOrder(id).Bind(order =>
                    {
                        try
                        {
                            return Result<Order>.Success(Save(order));
                        }
                        catch (Exception ex)
                        {
                            return Result<Order>.Failure(Error.FromException(ex));
                        }
                    });
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "OrderService.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC001", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Error, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_When_Using_TryExecuteAsync_Inside_Bind()
    {
        const string source = """
            using MonadicSharp;
            class OrderService
            {
                public Task<Result<Order>> Process(int id) =>
                    GetOrder(id).BindAsync(order =>
                        Try.ExecuteAsync(() => SaveAsync(order)));
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "OrderService.cs").ToList();

        Assert.Empty(findings);
    }
}
