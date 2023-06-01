using Console.Tools.Octopus.DeploymentTargets;
using Console.Tools.Octopus.Projects;
using Spectre.Console;
using System;
using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;

// todo: https://joshclose.github.io/CsvHelper/getting-started/

//Command Line Interface Guidelines
//interesting:  https://clig.dev/

//My journey of creating a .NET CLI tool
//https://www.faesel.com/blog/my-journey-of-creating-a-dotnet-cli-tool

//*************Spectre.console * ***********
//https://github.com/spectreconsole/spectre.console
//https://spectreconsole.net/cli/introduction
public class CommentsAndDeadCode
{
}



/*
 * confirmation = AnsiConsole.Confirm("Display markup as HTML?");
		AnsiConsole.Markup($"[green]{confirmation}[/]");
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		if (confirmation)
		{
			AnsiConsole.Markup($"[green]HTML MARKUP OF CONSOLE OUTPUT:[/]");
			AnsiConsole.WriteLine();
			AnsiConsole.WriteLine(AnsiConsole.ExportHtml());
		}
 */




//private void Display([NotNull] CreateSettings settings)
//{
//	foreach (var serverUrl in settings.ServerUrls)
//	{
//		Display("Server Url", serverUrl);
//	}

//	Display("Project Name", settings.ProjectName);
//	Display("Service Now Task Number", settings.ServiceNowTaskNumber);

//	foreach (var environment in settings.Environments ?? Array.Empty<string>())
//	{
//		Display("Environment", environment);
//	}
//}


//private static void Display(string propertyName, string? propertyValue)
//{
//	MarkupLine($":octopus: [blue]{propertyName}[/]: {propertyValue ?? string.Empty}");
//}




//bool shouldDisplaySettings = ConfirmSettingsOutput();

//if (shouldDisplaySettings)
//{
//	Display(settings);
//}




//private static void Hello([NotNull] DecommissionSettings settings)
//{
//	MarkupLine($"[underline green]Hello from[/] :octopus: Command Settings '{settings.GetType().Name}' :alien:!");
//	MarkupLine("[link]https://spectreconsole.net[/]");
//	MarkupLine("[link=https://spectreconsole.net]Spectre Console Documentation[/]");

//	// add blank lines for output readability
//	WriteLine();
//	WriteLine();
//}



//private static bool ConfirmSettingsOutput()
//{
//	return Confirm("Do you want to output the settings?");
//}


//// todo: use yaml serializer to output settings...
//private static void Display([NotNull] DecommissionSettings settings)
//{
//	var serializer = new SerializerBuilder().Build();
//	var yaml = serializer.Serialize(settings);

//	Write(
//		new FigletText("Command Settings")
//	.LeftJustified()
//	.Color(Color.Green));

//	Write(new Rule());

//	Status()
//		.Spinner(Spinner.Known.Dots)
//		.Start("automating all the things...", ctx => {
//			Thread.Sleep(2000);

//			MarkupLine(yaml);
//		});

//	Write(new Rule());

//	//foreach (var serverUrl in settings.ServerUrls)
//	//{
//	//	Display(
//	//		propertyName: "Octopus Server Url", 
//	//		propertyValue: serverUrl);
//	//}

//	//Display(
//	//		propertyName: "Deployment Target Name",
//	//		propertyValue: settings.DeploymentTargetName);

//	//Display(
//	//		propertyName: "Service Now Task Number",
//	//		propertyValue: settings.ServiceNowTaskNumber);
//}


//private static void Display(string propertyName, string? propertyValue)
//{
//	MarkupLine($":octopus: [blue]{propertyName}[/]: {propertyValue ?? string.Empty}");
//}