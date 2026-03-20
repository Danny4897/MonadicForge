using Microsoft.CodeAnalysis.CSharp;
using MonadicForge.Analyzer.Core;
using MonadicForge.Analyzer.Rules;

namespace MonadicForge.Analyzer.Tests.Rules;

public sealed class GC007Tests
{
    private readonly GC007_LlmOutputWithoutValidation _rule = new();

    // ── Syntactic analysis (no SemanticModel) ─────────────────────────────────

    [Fact]
    public void Detects_LlmOutput_Without_Validation()
    {
        // "CompleteAsync" is in StrongLlmMethods → flags even without SemanticModel
        const string source = """
            class Agent
            {
                public async Task<string> Run(string prompt)
                {
                    var result = await _chat.CompleteAsync(prompt);
                    return result.Content;
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Agent.cs").ToList();

        Assert.Single(findings);
        Assert.Equal("GC007", findings[0].RuleId);
        Assert.Equal(FindingSeverity.Error, findings[0].Severity);
    }

    [Fact]
    public void No_Finding_When_Validated_With_Ensure()
    {
        const string source = """
            class Agent
            {
                public async Task<Result<string>> Run(string prompt)
                {
                    return (await Try.ExecuteAsync(() => _chat.CompleteAsync(prompt)))
                        .Ensure(r => !string.IsNullOrEmpty(r.Content), "Empty LLM output");
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var findings = _rule.Analyze(tree, "Agent.cs").ToList();

        Assert.Empty(findings);
    }

    // ── SemanticModel analysis — false positive elimination ───────────────────

    [Fact]
    public void No_False_Positive_For_NonLlm_CreateAsync_With_SemanticModel()
    {
        // "CreateAsync" is in LlmOutputMethods but NOT in StrongLlmMethods.
        // Without SemanticModel: not flagged (conservative). With SemanticModel:
        // resolves to UserRepository (no LLM hint) → confirmed not flagged.
        const string source = """
            using System.Threading.Tasks;
            class User { public string Name { get; set; } = ""; }
            class CreateUserRequest { public string Name { get; set; } = ""; }
            class UserRepository
            {
                public Task<User> CreateAsync(CreateUserRequest request) =>
                    Task.FromResult(new User { Name = request.Name });
            }
            class Controller
            {
                private readonly UserRepository _repo = new();
                public async Task<User> Handle(CreateUserRequest req) =>
                    await _repo.CreateAsync(req);
            }
            """;

        var engine = new AnalysisEngine([_rule]);
        var result = engine.AnalyzeSource(source);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Detects_LlmOutput_Via_IChatClient_With_SemanticModel()
    {
        // Self-contained IChatClient interface — SemanticModel confirms it's an LLM type
        // because its name matches "IChatClient" in LlmTypeHints.
        const string source = """
            using System.Threading.Tasks;
            interface IChatClient
            {
                Task<string> CompleteAsync(string prompt);
            }
            class MyAgent
            {
                private readonly IChatClient _chat;
                public MyAgent(IChatClient chat) => _chat = chat;
                public async Task<string> Run(string prompt) =>
                    await _chat.CompleteAsync(prompt);
            }
            """;

        var engine = new AnalysisEngine([_rule]);
        var result = engine.AnalyzeSource(source);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, f => f.RuleId == "GC007");
    }
}
