using MonadicForge.Analyzer.Core;
using MonadicSharp;
using Spectre.Console;

namespace MonadicForge.Report;

public sealed class ConsoleReporter
{
    public Result<Unit> Render(
        IReadOnlyList<AnalysisFinding> findings,
        string path,
        int fileCount)
    {
        return Try.Execute(() =>
        {
            var scoreResult = GreenScoreCalculator.Calculate(findings);
            int score = scoreResult.IsSuccess ? scoreResult.Value : 0;

            // Header panel
            var panel = new Panel(
                $"[bold]MonadicForge Analysis — {Markup.Escape(path)}[/]\n" +
                $"Files analyzed: [bold]{fileCount}[/]   " +
                $"Issues: [bold]{findings.Count}[/]   " +
                $"Green score: {ScoreColor(score)}[bold]{score}[/][/]")
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(1, 0)
            };
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Findings table
            if (findings.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]No issues found. Perfect green score![/]");
                return Unit.Value;
            }

            var table = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Severity")
                .AddColumn("Rule")
                .AddColumn("Location")
                .AddColumn("Description");

            foreach (var f in findings.OrderByDescending(x => x.Severity))
            {
                table.AddRow(
                    SeverityMarkup(f.Severity),
                    $"[grey]{f.RuleId}[/]",
                    $"[italic]{Markup.Escape(System.IO.Path.GetFileName(f.FilePath))}:{f.Line}[/]",
                    Markup.Escape(f.Description));
            }
            AnsiConsole.Write(table);

            return Unit.Value;
        });
    }

    private static string ScoreColor(int score) => score switch
    {
        >= 90 => "[green]",
        >= 70 => "[yellow]",
        >= 50 => "[orange3]",
        _     => "[red]"
    };

    private static string SeverityMarkup(FindingSeverity severity) => severity switch
    {
        FindingSeverity.Error   => "[red]Error[/]",
        FindingSeverity.Warning => "[yellow]Warning[/]",
        FindingSeverity.Info    => "[blue]Info[/]",
        _                       => severity.ToString()
    };
}
