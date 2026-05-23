namespace ModMon.Cli;

internal sealed class DotnetInitSteps
{
	private readonly IDotnetCliRunner _dotnet;
	private readonly IConsole _console;

	public DotnetInitSteps(IDotnetCliRunner dotnet, IConsole console)
	{
		_dotnet = dotnet;
		_console = console;
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

		var kernelClass1 = Path.Combine(repoRoot, kernelProject, "Class1.cs");
		if (File.Exists(kernelClass1))
		{
			File.Delete(kernelClass1);
		}

		var apiClass1 = Path.Combine(repoRoot, apiProject, "Class1.cs");
		if (File.Exists(apiClass1))
		{
			File.Delete(apiClass1);
		}

		return 0;
	}
}
