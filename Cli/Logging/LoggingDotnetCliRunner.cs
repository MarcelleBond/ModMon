namespace ModMon.Cli;

internal sealed class LoggingDotnetCliRunner : IDotnetCliRunner
{
	private readonly IDotnetCliRunner _inner;
	private readonly ILogger _logger;

	public LoggingDotnetCliRunner(
		IDotnetCliRunner inner,
		ILogger logger)
	{
		_inner = inner;
		_logger = logger;
	}

	public async Task<int> RunAsync(
		string arguments,
		string workingDirectory)
	{
		_logger.LogInfo(
			$"dotnet {arguments} (in {workingDirectory})");
		var exitCode = await _inner.RunAsync(arguments, workingDirectory);
		_logger.LogDebug($"dotnet exited with code {exitCode}");
		return exitCode;
	}
}
