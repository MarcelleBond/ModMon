namespace ModMon.Cli;

internal sealed class ModuleAddCommandHandler
{
	private readonly IConsole _console;
	private readonly IDotnetCliRunner _dotnet;
	private readonly IFileSystemWriter _fileSystem;
	private readonly IOutputWriter _output;

	public ModuleAddCommandHandler(
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

	public async Task<int> HandleAsync(
		ModuleAddRequest request,
		IDryRunTracker tracker)
	{
		var scaffolder = new ModuleScaffolder(_console, _dotnet, _fileSystem);
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
