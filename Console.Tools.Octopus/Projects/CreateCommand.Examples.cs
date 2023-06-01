// See https://aka.ms/new-console-template for more information
using Spectre.Console.Cli;

namespace Console.Tools.Octopus.Projects;

/*
	# powershell
	.\Console.Tools.exe octopus project create `
		--server-url "http://www.nickniverson.com" `
		--project-name "test-name" `
		--service-now-task-number "TASK0000000000"
*/
public partial class CreateCommand : Command<CreateSettings> 
{
	public static string[] Example1 => new[]
	{
		"octopus",
		"project",
		"create",
		"--server-url",
		"\"https://example1.octopus.com\"",
		"--project-name",
		"\"example1\"",
		"--service-now-task-number",
		"\"TASK0000001\"",
		"--environments",
		"\"dev\"",
		"--environments",
		"\"test\"",
		"--environments",
		"\"staging\"",
		"--environments",
		"\"prod\"",
	};

	public static string[] Example2 => new[]
	{
		"octopus",
		"project",
		"create",
		"--server-url",
		"\"https://example2.octopus.com\"",
		"--project-name",
		"\"example2\"",
		"--service-now-task-number",
		"\"TASK0000002\"",
		"--environments",
		"\"dev\"",
		"--environments",
		"\"test\"",
		"--environments",
		"\"staging\"",
		"--environments",
		"\"prod\"",
	};
}
