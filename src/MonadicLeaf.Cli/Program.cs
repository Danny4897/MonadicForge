using MonadicLeaf.Cli.Commands;
using Spectre.Console;
using System.CommandLine;

var rootCommand = new RootCommand("MonadicLeaf — green-code guarantees for C# built on MonadicSharp");

rootCommand.AddCommand(AnalyzeCommand.Build());
rootCommand.AddCommand(MigrateCommand.Build());
rootCommand.AddCommand(ReportCommand.Build());

AnsiConsole.Write(new FigletText("MonadicLeaf").Color(Color.Cyan1));

return await rootCommand.InvokeAsync(args);
