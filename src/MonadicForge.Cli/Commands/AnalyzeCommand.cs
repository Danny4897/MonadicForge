using MonadicForge.Analyzer.Core;
using MonadicForge.Report;
using Spectre.Console;
using System.CommandLine;

namespace MonadicForge.Cli.Commands;

public static class AnalyzeCommand
{
    public static Command Build()
    {
        var pathOption = new Option<string>(
            name: "--path",
            description: "Path to analyze (file or directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var command = new Command("analyze", "Analyze C# code for green-code violations");
        command.AddOption(pathOption);

        command.SetHandler(path =>
        {
            AnsiConsole.MarkupLine($"[bold cyan]Analyzing:[/] {Markup.Escape(path)}");

            var engine = new AnalysisEngine();

            bool isFile = File.Exists(path) && path.EndsWith(".cs");
            var analyzeResult = isFile
                ? engine.AnalyzeFile(path)
                : engine.AnalyzePath(path);

            analyzeResult.Match(
                findings =>
                {
                    int fileCount = isFile
                        ? 1
                        : Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Count();

                    var reporter = new ConsoleReporter();
                    reporter.Render(findings, path, fileCount);
                },
                error =>
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error.Message)}");
                    Environment.Exit(1);
                });
        }, pathOption);

        return command;
    }
}
