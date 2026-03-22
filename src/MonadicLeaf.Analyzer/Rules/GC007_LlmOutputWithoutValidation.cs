using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Analyzer.Core;

namespace MonadicLeaf.Analyzer.Rules;

/// <summary>
/// GC007 — Output di LLM usato direttamente senza validazione al boundary
///
/// SemanticModel enhancement: verifies that the method being called belongs to a type
/// whose name or interfaces suggest it is an LLM/AI client. This eliminates false positives
/// from non-LLM services that happen to have methods named "CreateAsync", "CompleteAsync", etc.
/// (e.g., DbContext, factories, generic async services).
/// </summary>
public sealed class GC007_LlmOutputWithoutValidation : IAnalyzerRule
{
    public string RuleId => "GC007";
    public string Description => "Validate LLM output at the boundary with ValidatedResult.";
    public FindingSeverity Severity => FindingSeverity.Error;

    private static readonly HashSet<string> LlmOutputMethods =
    [
        "CompleteAsync", "GetChatMessageContentsAsync", "GenerateAsync",
        "CreateAsync", "CompleteChatAsync", "GetCompletionAsync"
    ];

    // Method names that are STRONGLY LLM-specific — flag even without SemanticModel confirmation
    private static readonly HashSet<string> StrongLlmMethods =
    [
        "CompleteAsync", "GetChatMessageContentsAsync", "CompleteChatAsync", "GetCompletionAsync"
    ];

    private static readonly HashSet<string> ValidationMethods =
    [
        "ValidatedResult", "ParseAs", "Validate", "Ensure", "Where", "Guard"
    ];

    // Type name fragments that indicate an LLM/AI client
    private static readonly HashSet<string> LlmTypeHints =
    [
        "ChatClient", "LanguageModel", "LlmClient", "AiClient", "AIClient",
        "ChatCompletionService", "TextGenerationService", "CompletionClient",
        "AnthropicClient", "OpenAIClient", "GeminiClient", "BedrockClient",
        "IChatClient", "ILanguageModel", "ILlmClient", "IAIClient"
    ];

    public IEnumerable<AnalysisFinding> Analyze(
        SyntaxTree tree,
        string filePath,
        SemanticModel? semanticModel = null)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this, semanticModel);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(
        SyntaxTree tree,
        string filePath,
        GC007_LlmOutputWithoutValidation rule,
        SemanticModel? semanticModel)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var methodName = node.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                _ => null
            };

            if (methodName is not null && LlmOutputMethods.Contains(methodName))
            {
                if (IsLlmCall(node, methodName) && !IsValidated(node))
                {
                    var pos = tree.GetLineSpan(node.Span);
                    Findings.Add(new AnalysisFinding(
                        rule.RuleId,
                        rule.Severity,
                        rule.Description,
                        filePath,
                        pos.StartLinePosition.Line + 1,
                        pos.StartLinePosition.Character + 1,
                        "result.Bind(output => output.ValidatedResult<MyModel>())"
                    ));
                }
            }
            base.VisitInvocationExpression(node);
        }

        /// <summary>
        /// Returns true if we can confirm (or cannot deny) this is an LLM call.
        /// With SemanticModel: confirms by checking the receiver type's name/interfaces.
        /// Without SemanticModel: strong method names always flag; ambiguous names (CreateAsync) skip.
        /// </summary>
        private bool IsLlmCall(InvocationExpressionSyntax node, string methodName)
        {
            if (semanticModel is null)
                // Conservative without model: only flag strongly LLM-specific method names
                return StrongLlmMethods.Contains(methodName);

            var symbolInfo = semanticModel.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (symbol is IMethodSymbol method)
            {
                var containingType = method.ContainingType;

                // Check the declaring type itself
                if (containingType is not null && IsLlmType(containingType))
                    return true;

                // Check if the receiver (object the method is called on) is LLM-typed
                if (node.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                    if (receiverType is INamedTypeSymbol namedReceiver && IsLlmType(namedReceiver))
                        return true;
                }

                // Symbol resolved but no LLM type found → confirmed not an LLM call
                return false;
            }

            // Symbol didn't resolve → fall back: strong names flag, ambiguous skip
            return StrongLlmMethods.Contains(methodName);
        }

        private static bool IsLlmType(INamedTypeSymbol type)
        {
            // Check the type's own name
            if (LlmTypeHints.Any(hint => type.Name.Contains(hint, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Check interfaces the type implements
            foreach (var iface in type.AllInterfaces)
            {
                if (LlmTypeHints.Any(hint => iface.Name.Contains(hint, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        private bool IsValidated(InvocationExpressionSyntax node)
        {
            var parent = node.Parent;
            int depth = 0;
            while (parent is not null && depth < 10)
            {
                if (parent is InvocationExpressionSyntax parentInv)
                {
                    var parentMethod = parentInv.Expression switch
                    {
                        MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                        _ => null
                    };
                    if (parentMethod is not null && ValidationMethods.Contains(parentMethod))
                        return true;
                }
                parent = parent.Parent;
                depth++;
            }
            return false;
        }
    }
}
