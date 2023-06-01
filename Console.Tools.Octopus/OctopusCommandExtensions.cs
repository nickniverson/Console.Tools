using Spectre.Console.Cli;

namespace Console.Tools.Octopus;

public static class OctopusCommandExtensions
{
	public static IConfigurator ConfigureOctopusCommands(this IConfigurator config)
	{
		config.AddBranch("octopus", octopus =>
		{
			octopus.AddBranch("deployment-target", deploymentTarget =>
			{
				deploymentTarget
					.AddCommand<DeploymentTargets.DecommissionCommand>("decommission")
					.WithExample(DeploymentTargets.DecommissionCommand.Example1)
					.WithExample(DeploymentTargets.DecommissionCommand.Example2);
			});

			octopus.AddBranch("project", project =>
			{
				project
					.AddCommand<Projects.CreateCommand>("create")
					.WithExample(Projects.CreateCommand.Example1)
					.WithExample(Projects.CreateCommand.Example2);
			});
		});

		return config;
	}
}
