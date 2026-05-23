namespace ModMon.Cli;

internal sealed class InitCommandHandler
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;

	public InitCommandHandler(
		IConsole console,
		IDotnetCliRunner dotnet,
		IFileSystemWriter fileSystem,
		IOutputWriter output)
	{
		_console = console;
		_dotnet = dotnet;
		_fileSystem = fileSystem;
		_output = output;
	}

	public async Task<int> HandleAsync(InitOptions options)
	{
		var repoRoot = string.IsNullOrWhiteSpace(options.OutputPath)
			? Directory.GetCurrentDirectory()
			: Path.GetFullPath(options.OutputPath);
		if (!_fileSystem.DirectoryExists(repoRoot))
		{
			Directory.CreateDirectory(repoRoot);
		}
		var solutionPath = Path.Combine(repoRoot, $"{options.ProjectName}.sln");
		if (_fileSystem.FileExists(solutionPath))
		{
			var errorResult = new InitResult(
				Success: false,
				Message: "Solution already exists",
				ExitCode: ExitCode.ResourceAlreadyExists,
				Timestamp: DateTime.UtcNow,
				ProjectName: options.ProjectName,
				SolutionPath: solutionPath,
				Errors: new List<string>
				{
					$"Solution file already exists: {solutionPath}"
				});
			_output.WriteError(errorResult.Message, errorResult);
			return (int)errorResult.ExitCode;
		}

		IDryRunTracker tracker = options.DryRun
			? new DryRunTracker(true)
			: new NullDryRunTracker();
		var init = new InitScaffolder(_console, _dotnet, _fileSystem);
		var result = await init.ScaffoldAsync(
			repoRoot,
			options.ProjectName,
			tracker);

		if (result.Success)
		{
			_output.WriteSuccess(result);
		}
		else
		{
			_output.WriteError(result.Message, result);
		}

		return (int)result.ExitCode;
	}
}
