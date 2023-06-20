using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Console.Tools.DotNet
{
	public enum ProjectType
	{
		Unknown,
		WebApi,
		ConsoleApp,
		AzureFunctionApp
	}

	public class ProjectMetadata
	{
		public string FullPath { get; private set; }

		public ProjectType Type { get; set; }

		public List<string> ProjectReferences { get; private set; } = new List<string>();

		public string? Sdk { get; set; }

		public string? OutputType { get; set; }

		public string? AzureFunctionsVersion { get; set; }

		public ProjectMetadata(string fullPath)
		{
			if (string.IsNullOrWhiteSpace(fullPath))
			{
				throw new ArgumentException($"'{nameof(fullPath)}' cannot be null or whitespace.", nameof(fullPath));
			}

			FullPath = fullPath;
		}
	}


	public interface IProjectTypeSpecification
	{
		ProjectType Type { get; }

		bool IsMatch(ProjectMetadata projectMetadata);
	}


	public class WebApiProjectSpecification : IProjectTypeSpecification
	{
		public ProjectType Type => ProjectType.WebApi;

		public bool IsMatch(ProjectMetadata projectMetadata)
		{
			return projectMetadata.Sdk == "Microsoft.NET.Sdk.Web";
		}
	}


	public class ConsoleAppProjectSpecification : IProjectTypeSpecification
	{
		public ProjectType Type => ProjectType.ConsoleApp;

		public bool IsMatch(ProjectMetadata projectMetadata)
		{
			return projectMetadata.OutputType == "Exe"
				&& projectMetadata.Sdk == "Microsoft.NET.Sdk";
		}
	}


	public class AzureFunctionAppProjectSpecification : IProjectTypeSpecification
	{
		public ProjectType Type => ProjectType.AzureFunctionApp;

		public bool IsMatch(ProjectMetadata projectMetadata)
		{
			return !string.IsNullOrEmpty(projectMetadata.AzureFunctionsVersion);
		}
	}



	public interface IProjectReader
	{
		ProjectMetadata ReadProject(string path);
		IEnumerable<ProjectMetadata> ReadProjects(string path);
	}



	public class ProjectReader : IProjectReader
	{
		private readonly IList<IProjectTypeSpecification> _projectTypeSpecifications;

		public ProjectReader(IList<IProjectTypeSpecification> projectTypeSpecifications)
		{
			_projectTypeSpecifications = projectTypeSpecifications ?? throw new ArgumentNullException(nameof(projectTypeSpecifications));
		}

		public ProjectMetadata ReadProject(string path)
		{
			var doc = XDocument.Load(path);
			var projectMetadata = new ProjectMetadata(path);

			projectMetadata.Sdk = doc.Descendants("Project").FirstOrDefault()?.Attribute("Sdk")?.Value;
			projectMetadata.OutputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;
			projectMetadata.AzureFunctionsVersion = doc.Descendants("AzureFunctionsVersion").FirstOrDefault()?.Value;
			projectMetadata.ProjectReferences.AddRange(
				doc
				.Descendants("ProjectReference")
				.Select(projectReference =>
					Path.GetFullPath(
						Path.Combine(
							Path.GetDirectoryName(path),
							projectReference.Attribute("Include").Value)
					))
				.ToList());

			SetProjectType(projectMetadata);

			return projectMetadata;
		}

		public IEnumerable<ProjectMetadata> ReadProjects(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException($"'{nameof(path)}' cannot be null or whitespace.", nameof(path));
			}

			var projectPaths = Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories);

			foreach (var projectPath in projectPaths)
			{
				var projectMetadata = ReadProject(projectPath);

				yield return projectMetadata;
			}
		}

		private void SetProjectType(ProjectMetadata projectMetadata)
		{
			foreach (var projectType in _projectTypeSpecifications)
			{
				if (projectType.IsMatch(projectMetadata))
				{
					projectMetadata.Type = projectType.Type;

					return;
				}
			}

			projectMetadata.Type = ProjectType.Unknown;
		}
	}

	public interface ISolutionCreator
	{
		void GenerateSolution(ProjectMetadata project);
	}

	public class SolutionCreator : ISolutionCreator
	{
		private readonly IProjectReader _projectReader;
		private readonly HashSet<string> _addedProjects = new HashSet<string>(); // Added to track projects already in the solution

		public SolutionCreator(IProjectReader projectReader)
		{
			_projectReader = projectReader ?? throw new ArgumentNullException(nameof(projectReader));
		}

		public void GenerateSolution(ProjectMetadata project)
		{
			_addedProjects.Clear();

			var solutionFileName = $"{Path.GetFileNameWithoutExtension(project.FullPath)}.CI.sln";
			var solutionFilePath = Path.Combine(Path.GetDirectoryName(project.FullPath), solutionFileName);

			// Delete the solution file if it already exists
			if (File.Exists(solutionFilePath))
			{
				File.Delete(solutionFilePath);
			}

			// Create a new solution file
			Dotnet($"new sln -n {Path.GetFileNameWithoutExtension(solutionFileName)} -o {Path.GetDirectoryName(project.FullPath)}");

			// Add the projects to the solution recursively
			AddProjectToSolution(solutionFilePath, project);
		}

		private void AddProjectToSolution(string solutionFilePath, ProjectMetadata project)
		{
			// Do not add a project if it has already been added
			if (_addedProjects.Contains(project.FullPath))
			{
				return;
			}

			// Add the project to the solution
			Dotnet($"sln {solutionFilePath} add {project.FullPath}");

			_addedProjects.Add(project.FullPath);  // Mark this project as added

			// Add the referenced projects to the solution
			foreach (var referencePath in project.ProjectReferences)
			{
				var referenceProject = _projectReader.ReadProject(referencePath);

				if (referenceProject != null)
				{
					AddProjectToSolution(solutionFilePath, referenceProject);
				}
			}
		}

		private void Dotnet(string arguments)
		{
			var process = new System.Diagnostics.Process
			{
				StartInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "dotnet",
					Arguments = arguments,
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			};

			process.Start();
			process.WaitForExit();
		}
	}


	public class AutoGeneratePerPackageSettings : CommandSettings
	{
		[CommandOption("-p|--path <path>")]
		public string? Path { get; set; }
	}


	public partial class AutoGeneratePerPackageCommand : Command<AutoGeneratePerPackageSettings>
	{
		private readonly IProjectReader _projectReader;
		private readonly ISolutionCreator _solutionCreator;


		public AutoGeneratePerPackageCommand(IProjectReader projectReader, ISolutionCreator solutionCreator)
		{
			_projectReader = projectReader ?? throw new ArgumentNullException(nameof(projectReader));
			_solutionCreator = solutionCreator ?? throw new ArgumentNullException(nameof(solutionCreator));
		}


		public override int Execute([NotNull] CommandContext context, [NotNull] AutoGeneratePerPackageSettings settings)
		{
			List<ProjectMetadata> projects = _projectReader
				.ReadProjects(settings.Path)
				.ToList();

			projects
				.Where(project => project.Type != ProjectType.Unknown)
				.ToList()
				.ForEach(project =>
				{
					AnsiConsole.WriteLine($"creating solution for project type:  '{project.Type}' with path '{project.FullPath}'");

					_solutionCreator.GenerateSolution(project);
				});

			return 0;
		}
	}


	public static class SolutionCommandExtensions
	{
		public static IConfigurator ConfigureSolutionCommands(this IConfigurator config)
		{
			config.AddBranch("solution", solution =>
			{
				solution.AddCommand<AutoGeneratePerPackageCommand>("auto-generate-per-package");
			});

			return config;
		}
	}
}