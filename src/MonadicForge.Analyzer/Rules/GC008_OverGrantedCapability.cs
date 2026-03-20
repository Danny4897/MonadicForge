using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonadicForge.Analyzer.Core;

namespace MonadicForge.Analyzer.Rules;

/// <summary>
/// GC008 — AgentCapability.All usato in produzione
///
/// Syntactic analysis is fully sufficient here: the rule detects the exact
/// member access expression "AgentCapability.All". This is an enum value with
/// a unique name — there is no realistic false positive scenario. SemanticModel
/// would not add value beyond confirming it's from MonadicSharp.Agents.Core,
/// which is an unnecessary overhead for a deterministic syntactic pattern.
/// </summary>
public sealed class GC008_OverGrantedCapability : IAnalyzerRule
{
    public string RuleId => "GC008";
    public string Description => "Grant minimum required capabilities only.";
    public FindingSeverity Severity => FindingSeverity.Warning;

    public IEnumerable<AnalysisFinding> Analyze(SyntaxTree tree, string filePath, SemanticModel? semanticModel = null)
    {
        var root = tree.GetRoot();
        var walker = new Walker(tree, filePath, this);
        walker.Visit(root);
        return walker.Findings;
    }

    private sealed class Walker(SyntaxTree tree, string filePath, GC008_OverGrantedCapability rule)
        : CSharpSyntaxWalker
    {
        public List<AnalysisFinding> Findings { get; } = [];

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name.Identifier.Text == "All" &&
                node.Expression is IdentifierNameSyntax { Identifier.Text: "AgentCapability" })
            {
                var pos = tree.GetLineSpan(node.Span);
                Findings.Add(new AnalysisFinding(
                    rule.RuleId,
                    rule.Severity,
                    rule.Description,
                    filePath,
                    pos.StartLinePosition.Line + 1,
                    pos.StartLinePosition.Character + 1,
                    "AgentCapability.ReadLocalFiles | AgentCapability.CallExternalApis  // only what you need"
                ));
            }
            base.VisitMemberAccessExpression(node);
        }
    }
}
