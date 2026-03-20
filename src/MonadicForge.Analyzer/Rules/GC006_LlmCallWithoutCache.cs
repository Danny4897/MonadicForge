using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC006 — Chiamate a LLM/AI senza CachingAgentWrapper</summary>
public sealed class GC006_LlmCallWithoutCache : IAnalyzerRule
{
    public string RuleId => "GC006";
    public string Description => "Wrap repeated LLM calls with CachingAgentWrapper.";
    public FindingSeverity Severity => FindingSeverity.Warning;

    // Type names that indicate LLM usage
    private static readonly HashSet<string> LlmTypeNames =
    [
        "IChatClient", "ILanguageModel", "IChatCompletionService",
        "ITextGenerationService", "OpenAIClient", "AnthropicClient",
        "IAIClient", "ILlmClient", "ChatClient"
    ];

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC006_LlmCallWithoutCache rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Check constructor parameters for LLM types
            var ctors = node.Members.OfType<ConstructorDeclarationSyntax>();
            bool hasLlmField = false;
            bool hasCachingWrapper = false;

            foreach (var ctor in ctors)
            {
                foreach (var param in ctor.ParameterList.Parameters)
                {
                    var typeName = param.Type?.ToString() ?? string.Empty;
                    if (LlmTypeNames.Any(t => typeName.Contains(t)))
                        hasLlmField = true;
                    if (typeName.Contains("CachingAgentWrapper") || typeName.Contains("ICacheService"))
                        hasCachingWrapper = true;
                }
            }

            // Also check field declarations
            foreach (var field in node.Members.OfType<FieldDeclarationSyntax>())
            {
                var typeName = field.Declaration.Type.ToString();
                if (LlmTypeNames.Any(t => typeName.Contains(t)))
                    hasLlmField = true;
                if (typeName.Contains("CachingAgentWrapper") || typeName.Contains("ICacheService"))
                    hasCachingWrapper = true;
            }

            if (hasLlmField && !hasCachingWrapper)
            {
                var pos = tree.GetLineSpan(node.Span);
                Findings.Add(new AnalysisFinding(
                    rule.RuleId,
                    rule.Severity,
                    rule.Description,
                    filePath,
                    pos.StartLinePosition.Line + 1,
                    pos.StartLinePosition.Character + 1,
                    "new CachingAgentWrapper<TIn, TOut>(innerAgent, _cache, policy)"
                ));
            }
            base.VisitClassDeclaration(node);
        }
    }
}
