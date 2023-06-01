// See https://aka.ms/new-console-template for more information
using Spectre.Console.Cli;

namespace Console.Tools.Octopus.DeploymentTargets;

/*
	# powershell
	.\Console.Tools.exe octopus deployment-target decommission `
		--server-url "http://www.nickniverson.com" `
		--deployment-target-name "test-name" `
		--service-now-task-number "TASK0000"
*/
public partial class DecommissionCommand : Command<DecommissionSettings>
{
	public static string[] Example1 => new[]
	{
		"octopus",
		"deployment-target",
		"decommission",
		"--server-url",
		"\"https://example1.octopus.com\"",
		"--deployment-target-name",
		"\"example1\"",
		"--service-now-task-number",
		"\"TASK0000001\""
	};


	public static string[] Example2 => new[]
	{
		"octopus",
		"deployment-target",
		"decommission",
		"--server-url",
		"\"https://example2.octopus.com\"",
		"--deployment-target-name",
		"\"example2\"",
		"--service-now-task-number",
		"\"TASK0000002\""
	};
}
