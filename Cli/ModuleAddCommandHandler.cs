namespace ModMon.Cli;

internal sealed class ModuleAddCommandHandler
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;
	private readonly ILogger _logger;

	public ModuleAddCommandHandler(
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

	public async Task<int> HandleAsync(
		ModuleAddRequest request,
		IDryRunTracker tracker)
	{
		var fileSystem = LoggingServices.Wrap(
			DryRunServices.Wrap(_fileSystem, tracker),
			_logger);
		var dotnet = LoggingServices.Wrap(
			DryRunServices.Wrap(_dotnet, tracker),
			_logger);
		var scaffolder = new ModuleScaffolder(_console, dotnet, fileSystem);
		var result = await scaffolder.ScaffoldAsync(
			request.RepoRoot,
			request,
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
