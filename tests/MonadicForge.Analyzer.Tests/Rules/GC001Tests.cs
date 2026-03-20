using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC001Tests
{
    private readonly GC001_TryCatchInsideBind _rule = new();

    // ── Syntactic analysis (no SemanticModel) ─────────────────────────────────

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

    // ── SemanticModel analysis — false positive elimination ───────────────────

    [Fact]
    public void No_Finding_For_UserDefined_Bind_With_SemanticModel()
    {
        // User's own Bind on a non-monadic type — should NOT trigger GC001
        // because the Bind method is NOT from MonadicSharp.
        const string source = """
            using System;
            class CustomPipeline
            {
                private readonly int _value;
                public CustomPipeline(int v) => _value = v;
                public CustomPipeline Bind(Func<int, CustomPipeline> f) => f(_value);
                public static CustomPipeline Start(int v) => new CustomPipeline(v);
            }
            class Test
            {
                public void Process()
                {
                    CustomPipeline.Start(42).Bind(x =>
                    {
                        try { return new CustomPipeline(x + 1); }
                        catch (Exception) { return new CustomPipeline(0); }
                    });
                }
            }
            """;

        var engine = new AnalysisEngine([_rule]);
        var result = engine.AnalyzeSource(source);

        Assert.True(result.IsSuccess);
        // With SemanticModel, GC001 is suppressed because Bind is not from MonadicSharp
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Still_Detects_When_MonadicSharp_Bind_Fully_Resolvable()
    {
        // Self-contained code using MonadicSharp Result<T>.Bind — should still flag
        const string source = """
            using MonadicSharp;
            using System;
            class Service
            {
                public static Result<int> Get() => Result<int>.Success(42);
                public static Result<int> Process() =>
                    Get().Bind(x =>
                    {
                        try { return Result<int>.Success(x * 2); }
                        catch (Exception ex) { return Result<int>.Failure(Error.FromException(ex)); }
                    });
            }
            """;

        var engine = new AnalysisEngine([_rule]);
        var result = engine.AnalyzeSource(source);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        Assert.Contains(result.Value, f => f.RuleId == "GC001");
    }
}
