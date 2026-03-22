using MonadicLeaf.Migrator.Core;
using Spectre.Console;
using System.CommandLine;

namespace MonadicLeaf.Cli.Commands;

public static class MigrateCommand
{
    public static Command Build()
    {
        var pathOption = new Option<string>(
            name: "--path",
            description: "Path to migrate (file or directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            description: "Show changes without writing files",
            getDefaultValue: () => false);

        var command = new Command("migrate", "Automatically rewrite C# code to MonadicSharp patterns");
        command.AddOption(pathOption);
        command.AddOption(dryRunOption);

        command.SetHandler((path, dryRun) =>
        {
            AnsiConsole.MarkupLine($"[bold cyan]Migrating:[/] {Markup.Escape(path)}{(dryRun ? " [yellow](dry-run)[/]" : string.Empty)}");

            var engine = new MigrationEngine();

            bool isFile = File.Exists(path) && path.EndsWith(".cs");

            if (dryRun && isFile)
            {
                var source = File.ReadAllText(path);
                var result = engine.MigrateSource(source, path);
                result.Match(
                    r =>
                    {
                        if (r.HasChanges)
                        {
                            AnsiConsole.MarkupLine($"[yellow]Would apply:[/] {string.Join(", ", r.AppliedRules)}");
                            AnsiConsole.Write(new Panel(r.NewSource ?? string.Empty) { Header = new PanelHeader("Preview") });
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[green]No changes needed.[/]");
                        }
                    },
                    error => AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error.Message)}"));
                return;
            }

            var migrateResult = isFile
                ? engine.MigrateFile(path).Map(r => (IReadOnlyList<MigrationResult>)[r])
                : engine.MigratePath(path);

            migrateResult.Match(
                results =>
                {
                    int changed = results.Count(r => r.HasChanges);
                    AnsiConsole.MarkupLine($"[green]Done.[/] {changed}/{results.Count} files modified.");

                    var table = new Table()
                        .Border(TableBorder.Simple)
                        .AddColumn("File")
                        .AddColumn("Rules Applied")
                        .AddColumn("Changes");

                    foreach (var r in results.Where(r => r.HasChanges))
                    {
                        table.AddRow(
                            Markup.Escape(Path.GetFileName(r.FilePath)),
                            string.Join(", ", r.AppliedRules),
                            r.ChangesApplied.ToString());
                    }

                    if (changed > 0) AnsiConsole.Write(table);
                },
                error =>
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(error.Message)}");
                    Environment.Exit(1);
                });
        }, pathOption, dryRunOption);

        return command;
    }
}
