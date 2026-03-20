using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>GC002 — .Map() chiamato su operazioni fallibili (I/O, parsing, deserializzazione)</summary>
public sealed class GC002_MapOnFallibleOperation : IAnalyzerRule
{
    public string RuleId => "GC002";
    public string Description => "Map is for infallible transforms. Use Bind + Try.Execute.";
    public FindingSeverity Severity => FindingSeverity.Error;

    // Indicators of fallible operations inside Map lambdas
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

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC002_MapOnFallibleOperation rule)
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
                        .OfType<InvocationExpressionSyntax>()
                        .Where(ContainsFallibleCall);

                    foreach (var inv in invocations)
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
            base.VisitInvocationExpression(node);
        }

        private static bool ContainsFallibleCall(InvocationExpressionSyntax inv)
        {
            var name = inv.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null
            };
            return name is not null && FallibleMethodNames.Contains(name);
        }
    }
}
