using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Console.Tools.DotNet
{
	// # REQUIREMENTS
	// should implement 2 modes:
	//	- auto
	//		- should automatically determine project types (this is how it is currently implemented)
	//	- explicit config
	//		- should accept an explicit list of projects that we want to build
	//		  solutions for (in the event that the auto-logic is faulty in some
	//		  way or if a new project type is added that hasn't been added to
	//		  this tool just yet)
	//  - should have option to put the new solutions at the repo root or alongside the project for which it was created
	//		- default should probably be next to the rest of the solutions or perhaps a configurable location?
	//		- I want it to be automatic rather than require explicit configuration... and having them closer to the root makes them more discoverable... which I prefer... (not sure of any downsides that I can think of)

	public enum ProjectType
	{
		Unknown,
		WebApi,
		ConsoleApp,
		AzureFunctionApp,
		TestProject,
	}


	public class ProjectMetadata
	{
		public string FullPath { get; private set; }

		public string Directory { get; private set; }

		public ProjectType Type { get; set; }

		public List<PackageReference> PackageReferences { get; private set; } = new List<PackageReference>();

		public List<ProjectReference> ProjectReferences { get; private set; } = new List<ProjectReference>();

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

			Directory = Path.GetDirectoryName(fullPath)!;
		}
	}


	public class PackageReference
	{
		public PackageReference(string include)
		{
			Include = include ?? throw new ArgumentNullException(nameof(include));
		}

		public string Include { get; set; }
	}


	public class ProjectReference
	{
		public ProjectReference(string include)
		{
			Include = include ?? throw new ArgumentNullException(nameof(include));
		}

		public string Include { get; set; }
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


	public class TestProjectSpecification : IProjectTypeSpecification
	{
		public ProjectType Type => ProjectType.TestProject;

		public bool IsMatch(ProjectMetadata projectMetadata)
		{
			return projectMetadata.PackageReferences.Any(x => x.Include == "Microsoft.NET.Test.Sdk");
		}
	}



	public interface IProjectReader
	{
		ProjectMetadata ReadProject(string projectFilePath);

		IEnumerable<ProjectMetadata> ReadProjects(string directoryPath);
	}



	public class ProjectReader : IProjectReader
	{
		private readonly IList<IProjectTypeSpecification> _projectTypeSpecifications;

		public ProjectReader(IList<IProjectTypeSpecification> projectTypeSpecifications)
		{
			_projectTypeSpecifications = projectTypeSpecifications ?? throw new ArgumentNullException(nameof(projectTypeSpecifications));
		}

		public ProjectMetadata ReadProject(string projectFilePath)
		{
			if (string.IsNullOrWhiteSpace(projectFilePath))
			{
				throw new ArgumentException($"'{nameof(projectFilePath)}' cannot be null or whitespace.", nameof(projectFilePath));
			}

			var doc = XDocument.Load(projectFilePath);
			var projectMetadata = new ProjectMetadata(projectFilePath);

			projectMetadata.Sdk = doc.Descendants("Project").FirstOrDefault()?.Attribute("Sdk")?.Value;
			projectMetadata.OutputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;
			projectMetadata.AzureFunctionsVersion = doc.Descendants("AzureFunctionsVersion").FirstOrDefault()?.Value;

			projectMetadata.PackageReferences.AddRange(
				doc
				.Descendants("PackageReference")
				.Select(packageReference => new PackageReference(
					packageReference.Attribute("Include")!.Value))
				.ToList());

			projectMetadata.ProjectReferences.AddRange(
				doc
				.Descendants("ProjectReference")
				.Select(projectReference =>
					new ProjectReference(
						include: Path.GetFullPath(
							Path.Combine(
								Path.GetDirectoryName(projectFilePath)!,
								projectReference.Attribute("Include")!.Value)
						)))
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
		void GenerateSolution(ProjectMetadata project, string rootDirectory);
	}


	public class SolutionCreator : ISolutionCreator
	{
		private readonly IProjectReader _projectReader;
		private readonly HashSet<string> _addedProjects = new(); // Added to track projects already in the solution
		private readonly List<ProjectMetadata> _allProjects = new();


		public SolutionCreator(IProjectReader projectReader)
		{
			_projectReader = projectReader ?? throw new ArgumentNullException(nameof(projectReader));
		}


		public void GenerateSolution(ProjectMetadata project, string rootDirectory)
		{
			if (project is null)
			{
				throw new ArgumentNullException(nameof(project));
			}

			if (string.IsNullOrEmpty(rootDirectory))
			{
				throw new ArgumentException($"'{nameof(rootDirectory)}' cannot be null or empty.", nameof(rootDirectory));
			}

			_addedProjects.Clear();
			_allProjects.Clear();

			// load all projects once
			_allProjects
				.AddRange(_projectReader
				.ReadProjects(rootDirectory));

			var solutionFileName = $"{Path.GetFileNameWithoutExtension(project.FullPath)}.CI.generated.sln";
			var solutionFilePath = Path.Combine(project.Directory, solutionFileName);

			// Delete the solution file if it already exists
			if (File.Exists(solutionFilePath))
			{
				File.Delete(solutionFilePath);
			}

			LogInfo(project, solutionFileName, solutionFilePath);

			// Create a new solution file
			Dotnet($"new sln -n {Path.GetFileNameWithoutExtension(solutionFileName)} -o {project.Directory}");

			// Add the projects to the solution recursively
			AddProjectToSolution(solutionFilePath, project);
		}


		private void LogInfo(ProjectMetadata project, string solutionFileName, string solutionFilePath)
		{
			AnsiConsole.MarkupLine($"[green]creating solution:[/] '{solutionFileName}'");
			AnsiConsole.MarkupLine($"	[green]for project type:[/] {project.Type}");
			AnsiConsole.MarkupLine($"	[green]for project path:[/] {project.FullPath}");
			AnsiConsole.MarkupLine($"	[green]at:[/] {solutionFilePath}");
			AnsiConsole.WriteLine();
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

			// Recursively add the referenced projects to the solution
			RecursivelyAddReferencedProjectsToSolution(solutionFilePath, project);

			// Recursively add test projects to the solution
			RecursivelyAddTestProjectsToSolution(solutionFilePath, project);
		}


		private void RecursivelyAddReferencedProjectsToSolution(string solutionFilePath, ProjectMetadata project)
		{
			foreach (var referencePath in project.ProjectReferences)
			{
				var referenceProject = _allProjects.FirstOrDefault(x => x.FullPath == referencePath.Include);

				if (referenceProject == null)
				{
					continue;
				}

				AddProjectToSolution(solutionFilePath, referenceProject);
			}
		}


		private void RecursivelyAddTestProjectsToSolution(string solutionFilePath, ProjectMetadata project)
		{
			IEnumerable<ProjectMetadata> testProjects = _allProjects.Where(project => project.Type == ProjectType.TestProject);

			foreach (var testProject in testProjects)
			{
				// if no references to the current project are found, continue
				if (!testProject.ProjectReferences.Any(projectReference => projectReference.Include == project.FullPath))
				{
					continue;
				}

				// if test project references current project, add it to the solution
				AddProjectToSolution(solutionFilePath, testProject);
			}
		}


		private void Dotnet(string arguments)
		{
			using var process = new Process
			{
				StartInfo = new ProcessStartInfo
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
			List<ProjectMetadata> allProjects = _projectReader
				.ReadProjects(settings.Path)
				.ToList();

			allProjects
				.Where(project => project.Type != ProjectType.Unknown && project.Type != ProjectType.TestProject)
				.ToList()
				.ForEach(project =>
				{
					_solutionCreator.GenerateSolution(project, rootDirectory: settings.Path);
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