using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicLeaf.Analyzer.Core;

namespace MonadicLeaf.Analyzer.Rules;

/// <summary>
/// GC009 — IAgent chiamato su servizi esterni senza CircuitBreaker
///
/// Structural class-level analysis: scans field/constructor parameter types for
/// known external service type names. SemanticModel could help resolve the full
/// interface hierarchy (e.g., confirming IHttpResultClient is from MonadicSharp.Http),
/// but the type name list is specific enough for low false-positive rates. A future
/// improvement: use SemanticModel to check if the field implements IAgent&lt;,&gt; and
/// is injected from outside the assembly boundary.
/// </summary>
public sealed class GC009_MissingCircuitBreaker : IAnalyzerRule
{
    public string RuleId => "GC009";
    public string Description => "Add CircuitBreaker on external agents.";
    public FindingSeverity Severity => FindingSeverity.Warning;

    // Type name fragments that indicate external service agents
    private static readonly HashSet<string> ExternalAgentTypeHints =
    [
        "HttpAgent", "ExternalAgent", "RemoteAgent",
        "IHttpResultClient", "HttpResultClient",
        "ApiAgent", "WebhookAgent", "SmtpAgent", "StorageAgent"
    ];

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath, SemanticModel? semanticModel = null)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC009_MissingCircuitBreaker rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            bool hasExternalAgent = false;
            bool hasCircuitBreaker = false;

            foreach (var field in node.Members.OfType<FieldDeclarationSyntax>())
            {
                var typeName = field.Declaration.Type.ToString();
                if (ExternalAgentTypeHints.Any(t => typeName.Contains(t)))
                    hasExternalAgent = true;
                if (typeName.Contains("CircuitBreaker"))
                    hasCircuitBreaker = true;
            }

            foreach (var ctor in node.Members.OfType<ConstructorDeclarationSyntax>())
            {
                foreach (var param in ctor.ParameterList.Parameters)
                {
                    var typeName = param.Type?.ToString() ?? string.Empty;
                    if (ExternalAgentTypeHints.Any(t => typeName.Contains(t)))
                        hasExternalAgent = true;
                    if (typeName.Contains("CircuitBreaker"))
                        hasCircuitBreaker = true;
                }
            }

            if (hasExternalAgent && !hasCircuitBreaker)
            {
                var pos = tree.GetLineSpan(node.Span);
                Findings.Add(new AnalysisFinding(
                    rule.RuleId,
                    rule.Severity,
                    rule.Description,
                    filePath,
                    pos.StartLinePosition.Line + 1,
                    pos.StartLinePosition.Character + 1,
                    "new CircuitBreaker(name: \"ServiceName\", failureThreshold: 5, openDuration: TimeSpan.FromSeconds(30))"
                ));
            }
            base.VisitClassDeclaration(node);
        }
    }
}
