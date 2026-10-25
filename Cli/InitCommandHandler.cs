namespace ModMon.Cli;

internal sealed class InitCommandHandler
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;
	private readonly ILogger _logger;

	public InitCommandHandler(
		IConsole console,
		IDotnetCliRunner dotnet,
		IFileSystemWriter fileSystem,
		IOutputWriter output,
		ILogger logger)
	{
		_console = console;
		_dotnet = dotnet;
		_fileSystem = fileSystem;
		_output = output;
		_logger = logger;
	}

	public async Task<int> HandleAsync(InitOptions options)
	{
		IDryRunTracker tracker = options.DryRun
			? new DryRunTracker(true)
			: new NullDryRunTracker();
		var fileSystem = LoggingServices.Wrap(
			DryRunServices.Wrap(_fileSystem, tracker),
			_logger);
		var dotnet = LoggingServices.Wrap(
			DryRunServices.Wrap(_dotnet, tracker),
			_logger);

		var repoRoot = string.IsNullOrWhiteSpace(options.OutputPath)
			? Directory.GetCurrentDirectory()
			: PathResolver.TryGetFullPath(options.OutputPath);
		if (repoRoot == null)
		{
			var invalid = new InitResult(
				Success: false,
				Message: $"Invalid --output path '{options.OutputPath}'.",
				ExitCode: ExitCode.InvalidArguments,
				Timestamp: DateTime.UtcNow,
				ProjectName: options.ProjectName,
				Errors: new List<string>
				{
					$"Invalid --output path '{options.OutputPath}'."
				});
			_output.WriteError(invalid.Message, invalid);
			return (int)invalid.ExitCode;
		}

		if (!fileSystem.DirectoryExists(repoRoot))
		{
			fileSystem.EnsureDirectory(repoRoot);
		}
		var solutionPath = Path.Combine(repoRoot, $"{options.ProjectName}.sln");
		var existingSln = fileSystem.DirectoryExists(repoRoot)
			? Directory.GetFiles(repoRoot, "*.sln").FirstOrDefault()
			: null;
		if (existingSln != null)
		{
			var errorResult = new InitResult(
				Success: false,
				Message: "Solution already exists",
				ExitCode: ExitCode.ResourceAlreadyExists,
				Timestamp: DateTime.UtcNow,
				ProjectName: options.ProjectName,
				SolutionPath: solutionPath,
				RepoRoot: repoRoot,
				Errors: new List<string>
				{
					$"Solution file already exists: {existingSln}"
				});
			_output.WriteError(errorResult.Message, errorResult);
			return (int)errorResult.ExitCode;
		}

		var init = new InitScaffolder(_console, dotnet, fileSystem);
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
