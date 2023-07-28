using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using Microsoft.CodeAnalysis;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;


public static class OctopusCommandExtensions
{
	public static IConfigurator ConfigureOctopusCommands(this IConfigurator config)
	{
		config.AddBranch("dotnet", octopus =>
		{
			octopus.AddBranch("generate", deploymentTarget =>
			{
				deploymentTarget.AddCommand<GenerateTestClassCommand>("test-classes");
			});
		});

		return config;
	}
}



//public class GenerateTestClassSettings : CommandSettings
//{
//	[CommandOption("-i|--input-folder-path <input-folder-path>")]
//	public string InputFolderPath { get; set; } = "C:\\code\\TestConsoleToolsSolutionCreator\\TestConsoleToolsSolutionCreator";

//	[CommandOption("-o|--output-folder-path <output-folder-path>")]
//	public string OutputFolderPath { get; set; } = "c:\\code\\test-class-generator-output";
//}

public class GenerateTestClassSettings : CommandSettings
{
	[CommandOption("-i|--input-folder-path <input-folder-path>")]
	public string InputFolderPath { get; set; } = "C:\\code\\budget-projection\\Source\\BudgetProjection.Core";

	[CommandOption("-o|--output-folder-path <output-folder-path>")]
	public string OutputFolderPath { get; set; } = "c:\\code\\test-class-generator-output";
}


public partial class GenerateTestClassCommand : Command<GenerateTestClassSettings>
{
	public override int Execute([NotNull] CommandContext context, [NotNull] GenerateTestClassSettings settings)
	{
		var driver = new TestClassGeneratorDriver(settings.InputFolderPath, settings.OutputFolderPath);

		driver.GenerateTestClasses();

		return 0;
	}
}


public class TestClassGeneratorDriver
{
	private readonly string _inputFolderPath;
	private readonly string _outputFolderPath;

	public TestClassGeneratorDriver(string inputFolderPath, string outputFolderPath)
	{
		_inputFolderPath = inputFolderPath;
		_outputFolderPath = outputFolderPath;
	}

	public void GenerateTestClasses()
	{
		// Find all the .cs files in the input directory and subdirectories
		var csFilePaths = Directory.GetFiles(_inputFolderPath, "*.cs", SearchOption.AllDirectories);

		foreach (var csFilePath in csFilePaths)
		{
			// Read the content of the .cs file
			var csFileContent = File.ReadAllText(csFilePath);

			// Generate the test class
			var testClassGenerator = new TestClassGeneratorV2(csFileContent);
			var testClassContent = testClassGenerator.GenerateTestClass();

			// if no code was generated, skip this file
			if (string.IsNullOrWhiteSpace(testClassContent))
				continue;

			// Compute the path of the output file
			var relativePath = Path.GetRelativePath(_inputFolderPath, csFilePath);
			var relativeDirectory = Path.GetDirectoryName(relativePath) ?? string.Empty;
			var fileNameNoExtension = Path.GetFileNameWithoutExtension(relativePath);
			var extension = Path.GetExtension(relativePath);
			var testFileName = $"{fileNameNoExtension}Tests{extension}";
			
			var testClassFilePath = Path.Combine(_outputFolderPath, relativeDirectory, testFileName);

			// Create the directory of the output file if it does not exist
			var testClassFileDirectoryPath = Path.GetDirectoryName(testClassFilePath);
			Directory.CreateDirectory(testClassFileDirectoryPath);

			// Write the content of the test class to the output file
			File.WriteAllText(testClassFilePath, testClassContent);
		}
	}
}






public class TestClassGeneratorV1
{
	private readonly CompilationUnitSyntax _root;
	private readonly StringBuilder _sb;
	private int _indentLevel;

	public TestClassGeneratorV1(string sourceCode)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		_root = syntaxTree.GetCompilationUnitRoot();
		_sb = new StringBuilder();
	}

	public string GenerateTestClass()
	{
		// skip files with no classes
		if (!_root.DescendantNodes().OfType<ClassDeclarationSyntax>().Any())
			return string.Empty;

		var classDeclaration = _root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
		var className = classDeclaration.Identifier.Text;
		var testClassName = $"{className}Tests";

		var publicMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
			.Where(m => m.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)));

		// skip classes with no public methods (DTO's/etc)
		if (!publicMethods.Any())
			return string.Empty;


		AppendLine($"[TestClass]");
		AppendLine($"public class {testClassName}");
		OpenBlock();
		{
			AppendLine("private IFixture _fixture;");
			AppendLine($"private {className} _sut; // System Under Test");

			// ... add other mock dependencies
			AppendLine();
			AppendLine();

			AppendLine("[TestInitialize]");
			AppendLine("public virtual void TestInitialize()");
			OpenBlock();
			{
				AppendLine("_fixture = new Fixture().Customize(new AutoMoqCustomization());");
				// ... freeze other mock dependencies
				AppendLine($"_sut = _fixture.Create<{className}>();");
			}
			CloseBlock();

			AppendLine();
			AppendLine();

			AppendLine("[TestCleanup]");
			AppendLine("public virtual void TestCleanup()");
			OpenBlock();
			{
				AppendLine($"// todo: any cleanup goes here");
			}
			CloseBlock();

			foreach (var method in publicMethods)
			{
				AppendLine();
				AppendLine();
				AppendLine();

				var methodName = method.Identifier.Text;

				AppendLine($"[TestClass]");
				AppendLine($"public class {methodName}Method : {testClassName}");
				OpenBlock();
				{
					AppendLine("[TestInitialize]");
					AppendLine($"public override void TestInitialize()");
					OpenBlock();
					{
						AppendLine("// Configure default behavior here...");
					}
					CloseBlock();

					AppendLine();
					AppendLine();

					AppendLine("[TestCleanup]");
					AppendLine($"public override void TestCleanup()");
					OpenBlock();
					{
						// ... reset all mocks
					}
					CloseBlock();

					AppendLine();
					AppendLine();

					AppendLine("[TestMethod]");
					AppendLine($"public void {methodName}_Test()");
					OpenBlock();
					{
						AppendLine("// Arrange");
						AppendLine("// ... create inputs using _fixture");
						AppendLine();
						AppendLine("// Act");
						AppendLine($"var result = _sut.{methodName}(); // modify this line to pass the inputs if any");
						AppendLine();
						AppendLine("// Assert");
						AppendLine("// ... assert something here");
					}
					CloseBlock();
				}
				CloseBlock();
			}
		}
		CloseBlock();

		return _sb.ToString();
	}

	private void OpenBlock()
	{
		AppendLine("{");
		_indentLevel++;
	}

	private void CloseBlock()
	{
		_indentLevel--;
		AppendLine("}");
	}

	private void AppendLine(string text = "")
	{
		_sb.AppendLine(new string('\t', _indentLevel) + text);
	}
}


/// <summary>
/// todo:  handle overloads causing duplicate method test classes
/// todo:  figure out how to merge output with an existing class without overwriting any existing methods (i.e. append to the class as more public methods are added)
/// </summary>
public class TestClassGeneratorV2
{
	private readonly CompilationUnitSyntax _root;
	private readonly StringBuilder _sb;
	private int _indentLevel;

	public TestClassGeneratorV2(string sourceCode)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
		_root = syntaxTree.GetCompilationUnitRoot();
		_sb = new StringBuilder();
	}

	public string GenerateTestClass()
	{
		// skip files with no classes
		if (!_root.DescendantNodes().OfType<ClassDeclarationSyntax>().Any())
			return string.Empty;

		var classDeclaration = _root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
		var className = classDeclaration.Identifier.Text;
		var testClassName = $"{className}Tests";

		var publicMethods = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
			.Where(m => m.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)));

		// skip classes with no public methods (DTO's/etc)
		if (!publicMethods.Any())
			return string.Empty;

		var constructorParameters = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>()
			.SelectMany(c => c.ParameterList.Parameters)
			.Select(p => p.Type.ToString())
			.ToList();

		AppendLine($"[TestClass]");
		AppendLine($"public class {testClassName}");
		OpenBlock();
		{
			AppendLine("private IFixture _fixture;");
			AppendLine($"private {className} _sut; // System Under Test");

			foreach (var param in constructorParameters)
			{
				AppendLine($"private Mock<{param}> _mock{param};");
			}

			AppendLine();
			AppendLine();

			AppendLine("[TestInitialize]");
			AppendLine("public virtual void TestInitialize()");
			OpenBlock();
			{
				AppendLine("_fixture = new Fixture().Customize(new AutoMoqCustomization());");
				foreach (var param in constructorParameters)
				{
					AppendLine($"_mock{param} = _fixture.Freeze<Mock<{param}>>();");
				}
				AppendLine($"_sut = _fixture.Create<{className}>();");
			}
			CloseBlock();

			AppendLine();
			AppendLine();

			AppendLine("[TestCleanup]");
			AppendLine("public virtual void TestCleanup()");
			OpenBlock();
			{
				AppendLine($"// todo: any cleanup goes here");
			}
			CloseBlock();

			foreach (var method in publicMethods)
			{
				AppendLine();
				AppendLine();
				AppendLine();
				var methodName = method.Identifier.Text;
				AppendLine($"[TestClass]");
				AppendLine($"public class {methodName}Method : {testClassName}");
				OpenBlock();
				{
					AppendLine("[TestInitialize]");
					AppendLine($"public override void TestInitialize()");
					OpenBlock();
					{
						AppendLine("base.TestInitialize();");
						AppendLine("// Configure default behavior here...");
					}
					CloseBlock();

					AppendLine();
					AppendLine();

					AppendLine("[TestCleanup]");
					AppendLine($"public override void TestCleanup()");
					OpenBlock();
					{
						AppendLine("base.TestCleanup();");
						// ... reset all mocks
					}
					CloseBlock();

					AppendLine();
					AppendLine();

					AppendLine("[TestMethod]");
					AppendLine($"public void {methodName}_Test()");
					OpenBlock();
					{
						AppendLine("// Arrange");
						AppendLine("// ... create inputs using _fixture");
						AppendLine();
						AppendLine("// Act");
						AppendLine($"var result = _sut.{methodName}(); // modify this line to pass the inputs if any");
						AppendLine();
						AppendLine("// Assert");
						AppendLine("// ... assert something here");
					}
					CloseBlock();
				}
				CloseBlock();
			}
		}
		CloseBlock();

		return _sb.ToString();
	}

	private void OpenBlock()
	{
		AppendLine("{");
		_indentLevel++;
	}

	private void CloseBlock()
	{
		_indentLevel--;
		AppendLine("}");
	}

	private void AppendLine(string text = "")
	{
		_sb.AppendLine(new string('\t', _indentLevel) + text);
	}
}



