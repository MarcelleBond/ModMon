namespace ModMon.Cli;

internal sealed class DotnetModuleSteps
{
	private readonly IDotnetCliRunner _dotnet;
	private readonly IConsole _console;

	public DotnetModuleSteps(IDotnetCliRunner dotnet, IConsole console)
	{
		_dotnet = dotnet;
		_console = console;
	}

	public async Task<int> RunAsync(
		string repoRoot,
		string projectName,
		string apiProject,
		string kernelProject,
		string moduleProject)
	{
		var commands = new[]
		{
			$"new classlib -n {moduleProject}",
			$"sln {projectName}.sln add {moduleProject}/{moduleProject}.csproj",
			$"add {apiProject}/{apiProject}.csproj reference {moduleProject}/{moduleProject}.csproj",
			$"add {moduleProject}/{moduleProject}.csproj reference {kernelProject}/{kernelProject}.csproj",
			$"add {moduleProject}/{moduleProject}.csproj package Microsoft.EntityFrameworkCore",
			$"add {moduleProject}/{moduleProject}.csproj package Microsoft.EntityFrameworkCore.Relational",
			$"add {moduleProject}/{moduleProject}.csproj package Microsoft.EntityFrameworkCore.Design",
			$"add {moduleProject}/{moduleProject}.csproj package Npgsql.EntityFrameworkCore.PostgreSQL",
			$"add {moduleProject}/{moduleProject}.csproj package DbUp",
			$"add {moduleProject}/{moduleProject}.csproj package dbup-postgresql"
		};

		foreach (var cmd in commands)
		{
			_console.WriteLine($"> dotnet {cmd}");
			var exit = await _dotnet.RunAsync(cmd, repoRoot);
			if (exit != 0)
			{
				return exit;
			}
		}

		var class1 = Path.Combine(repoRoot, moduleProject, "Class1.cs");
		if (File.Exists(class1))
		{
			File.Delete(class1);
		}

		EnsureDbUpScriptsEmbedded(repoRoot, moduleProject);

		return 0;
	}

	private static void EnsureDbUpScriptsEmbedded(
		string repoRoot,
		string moduleProject)
	{
		var csprojPath = Path.Combine(
			repoRoot,
			moduleProject,
			$"{moduleProject}.csproj");
		if (!File.Exists(csprojPath))
		{
			return;
		}

		var content = File.ReadAllText(csprojPath);
		if (content.Contains("Database\\Scripts\\**\\*.sql"))
		{
			return;
		}

		var marker = "</Project>";
		var index = content.LastIndexOf(
			marker,
			StringComparison.Ordinal);
		if (index < 0)
		{
			return;
		}

		var newLine = content.Contains("\r\n", StringComparison.Ordinal)
			? "\r\n"
			: "\n";
		var itemGroup = string.Join(
			newLine,
			"  <ItemGroup>",
			"    <EmbeddedResource Include=\"Database\\Scripts\\**\\*.sql\" />",
			"    <None Update=\"Database\\Scripts\\**\\*.sql\" CopyToOutputDirectory=\"Never\" />",
			"  </ItemGroup>",
			string.Empty);

		var updated = content.Insert(index, itemGroup + newLine);
		File.WriteAllText(csprojPath, updated);
	}
}
