using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;
using static Spectre.Console.AnsiConsole;

namespace Console.Tools.Octopus.DeploymentTargets;


public partial class DecommissionCommand : Command<DecommissionSettings>
{
	private readonly ICsvReader _csvReader;

	public DecommissionCommand(ICsvReader csvReader)
	{
		_csvReader = csvReader ?? throw new ArgumentNullException(nameof(csvReader));
	}

	public override int Execute([NotNull] CommandContext context, [NotNull] DecommissionSettings settings)
	{
		if (ContainsCsvFilePath(settings))
		{
			DecommissionOneDeploymentTarget(settings);
		}
		else if (!ContainsCsvFilePath(settings))
		{
			DecommissionMultipleDeploymentTargets(settings);
		}
		else
		{
			throw new NotSupportedException($"The given combination of arguments not supported for '{nameof(DecommissionCommand)}'");
		}

		return 0;
	}


	private static bool ContainsCsvFilePath([NotNull] DecommissionSettings settings)
	{
		return string.IsNullOrWhiteSpace(settings.CsvFilePath);
	}


	private static void DecommissionOneDeploymentTarget([NotNull] DecommissionSettings settings)
	{
		MarkupLine(":octopus: [green]TODO:  Needs Implemented:  DecommissionOneDeploymentTarget[/]");
	}


	private void DecommissionMultipleDeploymentTargets([NotNull] DecommissionSettings settings)
	{
		IEnumerable<DecommissionCsvRecord> records = _csvReader.ParseCsv(settings.CsvFilePathResolved);

		foreach (var record in records)
		{
			Write(record.ServiceNowTaskNumber);
			Write(",");
			WriteLine(record.DeploymentTargetName);
		}
	}
}
