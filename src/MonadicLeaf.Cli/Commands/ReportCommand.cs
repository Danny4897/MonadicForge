using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Report;
using Spectre.Console;
using System.CommandLine;

namespace MonadicLeaf.Cli.Commands;

public static class ReportCommand
{
    public static Command Build()
    {
        var pathOption = new Option<string>(
            name: "--path",
            description: "Path to analyze (file or directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var outputOption = new Option<string>(
            name: "--output",
            description: "Output HTML file path",
            getDefaultValue: () => "forge-report.html");

        var command = new Command("report", "Generate HTML security and green-score report");
        command.AddOption(pathOption);
        command.AddOption(outputOption);

        command.SetHandler((path, output) =>
        {
            AnsiConsole.MarkupLine($"[bold cyan]Generating report:[/] {Markup.Escape(path)}");

            var engine = new AnalysisEngine();

            bool isFile = File.Exists(path) && path.EndsWith(".cs");
            int fileCount = isFile
                ? 1
                : Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Count();

            var analyzeResult = isFile
                ? engine.AnalyzeFile(path)
                : engine.AnalyzePath(path);

            analyzeResult
                .Bind(findings =>
                {
                    // Console summary
                    new ConsoleReporter().Render(findings, path, fileCount);

                    // HTML report
                    var htmlReporter = new HtmlReporter();
                    return htmlReporter.WriteToFile(findings, path, output);
                })
                .Match(
                    _ => AnsiConsole.MarkupLine($"[green]Report saved:[/] {Markup.Escape(output)}"),
                    error =>
                    {
                        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error.Message)}");
                        Environment.Exit(1);
                    });
        }, pathOption, outputOption);

        return command;
    }
}
