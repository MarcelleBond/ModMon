namespace ModMon.Cli;

internal sealed class DotnetInitSteps
{
	private readonly IDotnetCliRunner _dotnet;
	private readonly IConsole _console;
	private readonly IFileSystemWriter _fileSystem;

	public DotnetInitSteps(
		IDotnetCliRunner dotnet,
		IConsole console,
		IFileSystemWriter fileSystem)
	{
		_dotnet = dotnet;
		_console = console;
		_fileSystem = fileSystem;
	}

	public async Task<int> RunAsync(
		string repoRoot,
		string projectName,
		string apiProject,
		string kernelProject)
	{
		var commands = new[]
		{
			$"new sln -n {projectName}",
			$"new webapi -n {apiProject}",
			$"new classlib -n {kernelProject}",
			$"sln {projectName}.sln add {apiProject}/{apiProject}.csproj",
			$"sln {projectName}.sln add {kernelProject}/{kernelProject}.csproj",
			$"add {apiProject}/{apiProject}.csproj reference {kernelProject}/{kernelProject}.csproj",
			$"add {apiProject}/{apiProject}.csproj package Swashbuckle.AspNetCore",
			$"add {apiProject}/{apiProject}.csproj package Serilog.AspNetCore",
			$"add {apiProject}/{apiProject}.csproj package Microsoft.EntityFrameworkCore",
			$"add {apiProject}/{apiProject}.csproj package Microsoft.EntityFrameworkCore.Relational",
			$"add {kernelProject}/{kernelProject}.csproj package Serilog.AspNetCore",
			$"add {kernelProject}/{kernelProject}.csproj package Newtonsoft.Json"
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

		DeleteIfExists(
			Path.Combine(repoRoot, kernelProject, "Class1.cs"));
		DeleteIfExists(
			Path.Combine(repoRoot, apiProject, "Class1.cs"));

		return 0;
	}

	private void DeleteIfExists(string path)
	{
		if (_fileSystem.FileExists(path))
		{
			_fileSystem.DeleteFile(path);
		}
	}
}
