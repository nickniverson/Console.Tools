using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using static Spectre.Console.AnsiConsole;

namespace Console.Tools.Octopus.Projects;


public partial class CreateCommand : Command<CreateSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] CreateSettings settings)
	{
		MarkupLine($"[underline green]Hello from[/] :octopus: Command '{settings.GetType().Name}'!");
		WriteLine();
		WriteLine();

		MarkupLine(":octopus: [green]TODO:  Needs Implemented:  CreateCommand.Execute[/]");

		return 0;
	}
}