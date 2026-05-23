namespace ModMon.Cli;

internal sealed class EfMigrationSteps
{
	private readonly IDotnetCliRunner _dotnet;
	private readonly IConsole _console;

	public EfMigrationSteps(IDotnetCliRunner dotnet, IConsole console)
	{
		_dotnet = dotnet;
		_console = console;
	}

	public async Task<int> TryCreateInitialMigrationAsync(
		string repoRoot,
		string projectName,
		string moduleProject)
	{
		if (!await IsDotnetEfAvailableAsync(repoRoot))
		{
			_console.WriteError(
				"dotnet-ef not found. Install with: dotnet tool install --global dotnet-ef");
			return 1;
		}

		var apiProject = $"{projectName}.Api";
		var apiProjectFile = $"{apiProject}/{apiProject}.csproj";
		var addDesignExit = await _dotnet.RunAsync(
			$"add {apiProjectFile} package Microsoft.EntityFrameworkCore.Design",
			repoRoot);
		if (addDesignExit != 0)
		{
			return addDesignExit;
		}

		var cmd = string.Join(
			" ",
			"ef migrations add InitialCreate",
			"--output-dir Database/Migrations",
			$"--project {moduleProject}",
			$"--startup-project {apiProject}");

		_console.WriteLine($"> dotnet {cmd}");
		return await _dotnet.RunAsync(cmd, repoRoot);
	}

	private async Task<bool> IsDotnetEfAvailableAsync(string repoRoot)
	{
		var exit = await _dotnet.RunAsync("ef --version", repoRoot);
		return exit == 0;
	}
}
