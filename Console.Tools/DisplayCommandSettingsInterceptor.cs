// See https://aka.ms/new-console-template for more information
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static Spectre.Console.AnsiConsole;
/// <summary>
/// could use this to serialize commands to log analytics for auditing...
/// </summary>
public class DisplayCommandSettingsInterceptor : ICommandInterceptor
{
	private readonly ISerializer _serializer;

	public DisplayCommandSettingsInterceptor(ISerializer serializer)
	{
		_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	}

	public void Intercept(CommandContext context, CommandSettings settings)
	{
		MarkupLine($"[lime]intercepted:  {settings.GetType().FullName}[/]");
		WriteLine();

		Display(header: "Command Context", obj: context);

		Display(header: "Command Settings", obj: settings);

		Write(
			new FigletText("Results")
			.LeftJustified()
			.Color(Color.DarkSlateGray1));

		// causes and overlap and display bug when using the spinner... for some reason
		//Status()
		//	.Spinner(Spinner.Known.Dots)
		//	.Start("automating all the things...", ctx => {
		//		Thread.Sleep(2000);

		//		Clear();
		//	});
	}


	private void Display(string header, object obj)
	{
		Write(
			new FigletText(header)
			.LeftJustified()
			.Color(Color.DarkSlateGray1));

		Write(new Rule());

		string yaml = _serializer.Serialize(obj);

		WriteLine(yaml);

		Write(new Rule());
	}
}