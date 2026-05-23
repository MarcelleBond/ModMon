namespace ModMon.Cli;

internal sealed class ConsoleLogger : ILogger
{
	private readonly IConsole _console;
	private readonly bool _verbose;

	public ConsoleLogger(IConsole console, bool verbose)
	{
		_console = console;
		_verbose = verbose;
	}

	public void LogInfo(string message)
	{
		if (_verbose)
		{
			_console.WriteLine($"[INFO] {message}");
		}
	}

	public void LogDebug(string message)
	{
		if (_verbose)
		{
			_console.WriteLine($"[DEBUG] {message}");
		}
	}

	public void LogWarning(string message)
	{
		if (_verbose)
		{
			_console.WriteLine($"[WARN] {message}");
		}
	}

	public void LogError(string message)
	{
		_console.WriteError($"[ERROR] {message}");
	}
}
