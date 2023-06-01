// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using Spectre.Console.Cli;

namespace Console.Tools.Octopus.DeploymentTargets;

public class DecommissionSettings : OctopusSettings
{
	[CommandOption("-n|--deployment-target-name <deployment-target-name>")]
	public string? DeploymentTargetName { get; set; }

	[CommandOption("-c|--csv-file-path <csv-file-path>")]
	public string? CsvFilePath { get; set; }

	public string? CsvFilePathResolved => string.IsNullOrWhiteSpace(CsvFilePath) ? string.Empty : Path.GetFullPath(CsvFilePath);


	public override ValidationResult Validate()
	{
		var validationResult = base.Validate();
		if (!validationResult.Successful) 
		{
			return validationResult;
		}

		bool nothingWasProvided = string.IsNullOrWhiteSpace(DeploymentTargetName)
				&& string.IsNullOrWhiteSpace(ServiceNowTaskNumber)
				&& string.IsNullOrWhiteSpace(CsvFilePath);

		if (nothingWasProvided)
		{
			return ValidationResult.Error("VALIDATION FAILED:  nothing was provided... use --help to see usage options");
		}

		bool mutuallyExclusiveOptionsWereProvided =
			(
				!string.IsNullOrWhiteSpace(DeploymentTargetName)
				|| !string.IsNullOrWhiteSpace(ServiceNowTaskNumber)
			)
			&& !string.IsNullOrWhiteSpace(CsvFilePath);

		if (mutuallyExclusiveOptionsWereProvided)
		{
			return ValidationResult.Error($"VALIDATION FAILED:  CSV option is mutually exlusive... " +
				$"please provide either --{GetCommandOptionLongName<DecommissionSettings>(s => s.CsvFilePath)} OR the " +
				$"--{GetCommandOptionLongName<DecommissionSettings>(s => s.DeploymentTargetName)} and the " +
				$"--{GetCommandOptionLongName<DecommissionSettings>(s => s.ServiceNowTaskNumber)}... but not both");
		}

		bool csvDoesNotExist = !string.IsNullOrWhiteSpace(CsvFilePath) 
			&& !File.Exists(CsvFilePathResolved);

		if (csvDoesNotExist)
		{
			return ValidationResult.Error($"VALIDATION FAILED:  CSV file '{CsvFilePath}' does not exist.  CSV file path resolved to:  '{CsvFilePathResolved}'");
		}

		return ValidationResult.Success();
	}
}
