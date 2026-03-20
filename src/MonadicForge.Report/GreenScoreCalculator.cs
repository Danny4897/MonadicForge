using MonadicForge.Analyzer.Core;
using MonadicSharp;

namespace MonadicForge.Report;

public static class GreenScoreCalculator
{
    private const int ErrorPenalty   = 10;
    private const int WarningPenalty = 5;
    private const int InfoPenalty    = 1;

    public static Result<int> Calculate(IReadOnlyList<AnalysisFinding> findings)
    {
        return Try.Execute(() =>
        {
            int penalty = findings.Sum(f => f.Severity switch
            {
                FindingSeverity.Error   => ErrorPenalty,
                FindingSeverity.Warning => WarningPenalty,
                FindingSeverity.Info    => InfoPenalty,
                _                       => 0
            });
            return Math.Max(0, 100 - penalty);
        });
    }

    /// <summary>Returns the 3 findings that, if fixed, improve the score the most.</summary>
    public static Result<IReadOnlyList<AnalysisFinding>> QuickWins(
        IReadOnlyList<AnalysisFinding> findings)
    {
        return Try.Execute(() =>
        {
            return (IReadOnlyList<AnalysisFinding>)findings
                .OrderByDescending(f => f.Severity)
                .Take(3)
                .ToList();
        });
    }
}
