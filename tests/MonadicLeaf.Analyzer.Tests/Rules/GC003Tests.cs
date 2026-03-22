using Microsoft.CodeAnalysis.CSharp;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Analyzer.Rules;

namespace MonadicLeaf.Analyzer.Tests.Rules;

public sealed class GC003Tests
{
    private readonly GC003_ExpensiveBeforeValidation _rule = new();

    [Fact]
    public void Detects_Expensive_Before_Validation_In_Chain()
    {
        const string source = """
            class Service
            {
                public Task Process(Query q) =>
                    _repo.FindAsync(q.Id)
                        .Where(x => x != null);
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Service.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC003", findings[0].RuleId);
    }

    [Fact]
    public void No_Finding_When_Validation_Before_Expensive()
    {
        const string source = """
            class Service
            {
                public async Task Process(Query q) =>
                    Result<Query>.Success(q)
                        .Where(x => x.Id > 0)
                        .BindAsync(x => _repo.FindAsync(x.Id));
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Service.cs").ToList();

        Assert.Empty(findings);
    }
}
