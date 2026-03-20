using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>
/// GC002 — .Map() chiamato su operazioni fallibili (I/O, parsing, deserializzazione)
///
/// SemanticModel enhancements:
/// 1. Detects async methods inside Map (method returns Task&lt;T&gt;) — always an error regardless of name.
/// 2. Reduces false positives: if a user method is in their own namespace (not System.IO/Net/Data)
///    and the symbol is clearly a user-defined utility, it is downgraded or skipped.
/// 3. If the containing namespace is a known I/O namespace, flags regardless of method name.
/// </summary>
public sealed class GC002_MapOnFallibleOperation : IAnalyzerRule
{
    public string RuleId => "GC002";
    public string Description => "Map is for infallible transforms. Use Bind + Try.Execute.";
    public FindingSeverity Severity => FindingSeverity.Error;

    private static readonly HashSet<string> FallibleMethodNames =
    [
        "Parse", "ParseExact", "TryParse",
        "Deserialize", "DeserializeObject", "ReadAllText", "ReadAllLines",
        "ReadAllBytes", "ReadToEnd", "GetString", "Open", "OpenRead",
        "OpenWrite", "Create", "Delete", "Move", "Copy",
        "GetResponse", "Send", "SendAsync", "Execute", "ExecuteAsync",
        "Connect", "Query", "QueryAsync", "SaveChanges", "SaveChangesAsync",
        "ToObject", "FromJson", "Convert"
    ];

    // Namespaces where any method call is considered I/O and therefore fallible
    private static readonly HashSet<string> DangerousNamespacePrefixes =
    [
        "System.IO",
        "System.Net",
        "System.Data",
        "System.Runtime.InteropServices",
        "System.Xml",
        "System.Text.Json",
        "Newtonsoft.Json"
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
        GC002_MapOnFallibleOperation rule,
        SemanticModel? semanticModel)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Map" or "MapAsync" })
            {
                foreach (var arg in node.ArgumentList.Arguments)
                {
                    var invocations = arg.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>();

                    foreach (var inv in invocations)
                    {
                        if (IsFallible(inv))
                        {
                            var pos = tree.GetLineSpan(node.Span);
                            Findings.Add(new AnalysisFinding(
                                rule.RuleId,
                                rule.Severity,
                                rule.Description,
                                filePath,
                                pos.StartLinePosition.Line + 1,
                                pos.StartLinePosition.Character + 1,
                                ".Bind(x => Try.Execute(() => riskyOp(x)))"
                            ));
                            break; // one finding per Map call
                        }
                    }
                }
            }
            base.VisitInvocationExpression(node);
        }

        private bool IsFallible(InvocationExpressionSyntax inv)
        {
            if (semanticModel is not null)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(inv);
                var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                if (symbol is IMethodSymbol method)
                {
                    // Async methods inside Map are always wrong (produces Result<Task<T>>)
                    if (method.IsAsync)
                        return true;
                    if (method.ReturnType is INamedTypeSymbol rt &&
                        (rt.Name == "Task" || rt.Name == "ValueTask"))
                        return true;

                    // Confirmed I/O namespace → always fallible
                    var ns = method.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                    if (DangerousNamespacePrefixes.Any(p => ns.StartsWith(p, StringComparison.Ordinal)))
                        return true;

                    // Resolved to a user-defined namespace (not System.*) with a name-matched method:
                    // reduce false positives — only flag if the name strongly suggests I/O
                    if (!ns.StartsWith("System", StringComparison.Ordinal) &&
                        !ns.StartsWith("Microsoft", StringComparison.Ordinal))
                    {
                        // For user-defined methods we only flag if the name is in a strict subset
                        // (the generic names like "Convert", "Create" are suppressed for user types)
                        return StrictFallibleMethodNames.Contains(GetMethodName(inv));
                    }

                    // For System.* methods, use name-based check
                    return FallibleMethodNames.Contains(GetMethodName(inv));
                }
                // Symbol didn't resolve — fall through to syntactic check
            }

            return FallibleMethodNames.Contains(GetMethodName(inv));
        }

        // Strict subset: only flag user-defined methods with clearly dangerous names
        private static readonly HashSet<string> StrictFallibleMethodNames =
        [
            "Parse", "ParseExact",
            "Deserialize", "DeserializeObject",
            "ReadAllText", "ReadAllLines", "ReadAllBytes", "ReadToEnd",
            "Open", "OpenRead", "OpenWrite",
            "GetResponse", "SendAsync", "QueryAsync", "SaveChangesAsync"
        ];

        private static string? GetMethodName(InvocationExpressionSyntax inv) =>
            inv.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            };
    }
}
