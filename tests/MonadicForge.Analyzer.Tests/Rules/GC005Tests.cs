using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC005Tests
{
    private readonly GC005_RetryWrapsValidation _rule = new();

    [Fact]
    public void Detects_Validation_Inside_Retry_Scope()
    {
        const string source = """
            class Service
            {
                public Task<Result<Order>> Process(Command cmd) =>
                    pipeline.ThenWithRetry(x =>
                        Result<Command>.Success(x)
                            .Ensure(c => c.Amount > 0, "Amount must be positive")
                            .BindAsync(c => _api.SendAsync(c)),
                        maxAttempts: 3);
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Service.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC005", findings[0].RuleId);
    }

    [Fact]
    public void No_Finding_When_Validation_Outside_Retry()
    {
        const string source = """
            class Service
            {
                public Task<Result<Order>> Process(Command cmd) =>
                    Result<Command>.Success(cmd)
                        .Ensure(c => c.Amount > 0, "Amount must be positive")
                        .BindAsync(c => pipeline.ThenWithRetry(
                            x => _api.SendAsync(x), maxAttempts: 3));
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Service.cs").ToList();

        Assert.Empty(findings);
    }
}
