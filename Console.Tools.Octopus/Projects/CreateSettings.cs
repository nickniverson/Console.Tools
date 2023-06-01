// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using Spectre.Console.Cli;

namespace Console.Tools.Octopus.Projects;

/* 
	---------------------
	goal: ideal usage  
	---------------------
	octopus project create `
	--octopus-server-url <octopus-server-url> `
	--deployment-target-name <deployment-target-name> `
	--service-now-task-number <service-now-task-number> 
*/
public class CreateSettings : OctopusSettings
{
	[CommandOption("-n|--project-name <project-name>")]
	public string? ProjectName { get; set; }

	[CommandOption("-e|--environments <environments>")]
	public string[]? Environments { get; set; }


	public override ValidationResult Validate()
	{
		if (ServerUrls.Length > 1)
		{
			string errorMessage = $"VALIDATION FAILED:  Only ONE {GetCommandOptionLongName<CreateSettings>(x => x.ServerUrls)} can be provided for this command";

			return ValidationResult.Error(errorMessage);
		}

		return ValidationResult.Success();
	}
}
