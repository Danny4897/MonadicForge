using MonadicLeaf.Analyzer.Core;
using MonadicSharp;
using System.Text;

namespace MonadicLeaf.Report;

public sealed class HtmlReporter
{
    public Result<string> GenerateHtml(
        IReadOnlyList<AnalysisFinding> findings,
        string path)
    {
        return GreenScoreCalculator.Calculate(findings)
               .Bind(score => GreenScoreCalculator.QuickWins(findings)
               .Map(quickWins => BuildHtml(findings, quickWins, score, path)));
    }

    public Result<Unit> WriteToFile(
        IReadOnlyList<AnalysisFinding> findings,
        string path,
        string outputFile)
    {
        return GenerateHtml(findings, path)
               .Bind(html => Try.Execute(() =>
               {
                   File.WriteAllText(outputFile, html, Encoding.UTF8);
                   return Unit.Value;
               }));
    }

    private static string BuildHtml(
        IReadOnlyList<AnalysisFinding> findings,
        IReadOnlyList<AnalysisFinding> quickWins,
        int score,
        string path)
    {
        var sb = new StringBuilder();
        var scoreColor = score >= 90 ? "#22c55e" : score >= 70 ? "#eab308" : score >= 50 ? "#f97316" : "#ef4444";

        sb.AppendLine($$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
              <title>MonadicLeaf Report — {{EscapeHtml(path)}}</title>
              <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                        background: #0f172a; color: #e2e8f0; padding: 2rem; }
                h1 { font-size: 1.8rem; margin-bottom: 0.5rem; color: #f1f5f9; }
                h2 { font-size: 1.2rem; margin: 2rem 0 1rem; color: #94a3b8; text-transform: uppercase;
                      letter-spacing: 0.05em; border-bottom: 1px solid #334155; padding-bottom: 0.5rem; }
                .meta { color: #64748b; margin-bottom: 2rem; }
                .gauge-wrap { display: flex; align-items: center; gap: 2rem; margin: 1.5rem 0; }
                .gauge { position: relative; width: 120px; height: 120px; }
                .gauge svg { transform: rotate(-90deg); }
                .gauge-label { position: absolute; inset: 0; display: flex; align-items: center;
                                justify-content: center; font-size: 1.6rem; font-weight: 700;
                                color: {{scoreColor}}; transform: rotate(0deg); }
                .score-desc { font-size: 1rem; color: #94a3b8; }
                table { width: 100%; border-collapse: collapse; margin-top: 0.5rem; }
                th { text-align: left; padding: 0.6rem 0.8rem; background: #1e293b;
                      color: #64748b; font-size: 0.75rem; text-transform: uppercase;
                      letter-spacing: 0.05em; }
                td { padding: 0.6rem 0.8rem; border-bottom: 1px solid #1e293b;
                      font-size: 0.875rem; vertical-align: top; }
                tr:hover td { background: #1e293b; }
                .badge { display: inline-block; padding: 2px 8px; border-radius: 999px;
                          font-size: 0.75rem; font-weight: 600; }
                .error   { background: #450a0a; color: #fca5a5; }
                .warning { background: #422006; color: #fcd34d; }
                .info    { background: #0c1a2e; color: #7dd3fc; }
                .suggestion { font-family: 'Courier New', monospace; font-size: 0.8rem;
                               color: #22d3ee; background: #0f2133; padding: 0.3rem 0.6rem;
                               border-radius: 4px; display: block; margin-top: 4px; }
                .quickwin { background: #0f2133; border-left: 3px solid #22d3ee;
                             padding: 0.8rem 1rem; margin-bottom: 0.8rem; border-radius: 0 4px 4px 0; }
                .quickwin .rule { font-weight: 700; color: #22d3ee; }
                .quickwin .desc { color: #94a3b8; font-size: 0.875rem; margin-top: 4px; }
              </style>
            </head>
            <body>
              <h1>MonadicLeaf Analysis Report</h1>
              <p class="meta">Path: {{EscapeHtml(path)}} &nbsp;|&nbsp; Generated: {{DateTime.UtcNow:yyyy-MM-dd HH:mm}} UTC</p>
            """);

        // Gauge
        var circumference = 2 * Math.PI * 45;
        var dashOffset = circumference * (1 - score / 100.0);
        sb.AppendLine($$"""
              <div class="gauge-wrap">
                <div class="gauge">
                  <svg width="120" height="120" viewBox="0 0 120 120">
                    <circle cx="60" cy="60" r="45" fill="none" stroke="#1e293b" stroke-width="10"/>
                    <circle cx="60" cy="60" r="45" fill="none" stroke="{{scoreColor}}" stroke-width="10"
                            stroke-dasharray="{{circumference:F1}}"
                            stroke-dashoffset="{{dashOffset:F1}}"
                            stroke-linecap="round"/>
                  </svg>
                  <div class="gauge-label">{{score}}</div>
                </div>
                <div class="score-desc">
                  <strong>Green Score: {{score}}/100</strong><br/>
                  Files: {{findings.Select(f => f.FilePath).Distinct().Count()}} &nbsp;
                  Issues: {{findings.Count}} &nbsp;
                  Errors: {{findings.Count(f => f.Severity == FindingSeverity.Error)}} &nbsp;
                  Warnings: {{findings.Count(f => f.Severity == FindingSeverity.Warning)}}
                </div>
              </div>
            """);

        // Quick wins
        if (quickWins.Any())
        {
            sb.AppendLine("  <h2>Quick Wins</h2>");
            foreach (var f in quickWins)
            {
                sb.AppendLine($$"""
                      <div class="quickwin">
                        <div class="rule">{{f.RuleId}} — {{EscapeHtml(f.Description)}}</div>
                        <div class="desc">{{EscapeHtml(Path.GetFileName(f.FilePath))}}:{{f.Line}}</div>
                        <code class="suggestion">{{EscapeHtml(f.Suggestion)}}</code>
                      </div>
                    """);
            }
        }

        // Findings table
        sb.AppendLine("  <h2>All Findings</h2>");
        if (findings.Count == 0)
        {
            sb.AppendLine("  <p style='color:#22c55e'>No issues found.</p>");
        }
        else
        {
            sb.AppendLine("""
                  <table>
                    <thead><tr>
                      <th>Severity</th><th>Rule</th><th>Location</th>
                      <th>Description</th><th>Suggestion</th>
                    </tr></thead>
                    <tbody>
                """);

            foreach (var f in findings.OrderByDescending(x => x.Severity).ThenBy(x => x.FilePath).ThenBy(x => x.Line))
            {
                var cls = f.Severity.ToString().ToLower();
                sb.AppendLine($$"""
                          <tr>
                            <td><span class="badge {{cls}}">{{f.Severity}}</span></td>
                            <td><strong>{{f.RuleId}}</strong></td>
                            <td>{{EscapeHtml(Path.GetFileName(f.FilePath))}}:{{f.Line}}</td>
                            <td>{{EscapeHtml(f.Description)}}</td>
                            <td><code class="suggestion">{{EscapeHtml(f.Suggestion)}}</code></td>
                          </tr>
                    """);
            }

            sb.AppendLine("    </tbody></table>");
        }

        sb.AppendLine("""
            </body>
            </html>
            """);

        return sb.ToString();
    }

    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
