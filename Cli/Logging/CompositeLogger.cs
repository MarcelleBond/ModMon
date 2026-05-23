namespace ModMon.Cli;

internal sealed class CompositeLogger : ILogger
{
	private readonly List<ILogger> _loggers;

	public CompositeLogger(params ILogger[] loggers)
	{
		_loggers = new List<ILogger>(loggers);
	}

	public void LogInfo(string message)
	{
		foreach (var logger in _loggers)
		{
			logger.LogInfo(message);
		}
	}

	public void LogDebug(string message)
	{
		foreach (var logger in _loggers)
		{
			logger.LogDebug(message);
		}
	}

	public void LogWarning(string message)
	{
		foreach (var logger in _loggers)
		{
			logger.LogWarning(message);
		}
	}

	public void LogError(string message)
	{
		foreach (var logger in _loggers)
		{
			logger.LogError(message);
		}
	}
}
