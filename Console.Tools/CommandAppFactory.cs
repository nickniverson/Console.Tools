// See https://aka.ms/new-console-template for more information
using Autofac;
using Console.Tools.Octopus;
using Spectre.Console;
using Spectre.Console.Cli;
using Console.Tools;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Autofac.Core;

/// <summary>
/// Encapsulates Spectre.Console and Autofac DI Container bootstrapping
/// https://github.com/autofac/Autofac/issues/811
/// </summary>
internal static class CommandAppFactory
{
	/// <summary>
	///  Creates a CommandApp instance and configures Autofac DI.
	/// </summary>
	internal static ICommandApp Create()
	{
		var builder = new ContainerBuilder();

		builder
			.RegisterAssemblyTypes(typeof(Program).Assembly)
			.AsImplementedInterfaces()
			.OnRegistered(LogToConsole);

		builder
			.RegisterAssemblyTypes(typeof(OctopusCommandExtensions).Assembly)
			.AsImplementedInterfaces()
			.OnRegistered(LogToConsole);

		builder
			.Register(c =>
				{
					ISerializer serializer = new SerializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();

					return serializer;
				})
			.As<ISerializer>()
			.SingleInstance();


		return new CommandApp(new AutofacTypeRegistrar(builder));
	}


	/// <summary>
	/// Configures Spectre.Console command app options... 
	/// Sets up interception, etc...
	/// Configures Octopus Commands... 
	/// </summary>
	internal static ICommandApp Configure(this ICommandApp app)
	{
		app.Configure(config =>
		{
#if DEBUG
			config.ValidateExamples();
#endif
			config
				.CaseSensitivity(CaseSensitivity.None)
				.SetApplicationName("Console.Tools")
				.SetInterceptor(CreateDisplayCommandSettingsInterceptor())
				.SetExceptionHandler(exception => AnsiConsole.WriteException(exception, ExceptionFormats.Default))

				//extension method to configure octopus commands... 
				.ConfigureOctopusCommands();
		});

		return app;
	}

	private static DisplayCommandSettingsInterceptor CreateDisplayCommandSettingsInterceptor()
	{
		var serializer = new SerializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();

		return new DisplayCommandSettingsInterceptor(serializer);
	}

	private static void LogToConsole(ComponentRegisteredEventArgs e)
	{
		AnsiConsole.MarkupLine($"Registered: {e.ComponentRegistration.Activator.LimitType.Name}");
	}
}