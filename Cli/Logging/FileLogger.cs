namespace ModMon.Cli;

internal sealed class FileLogger : ILogger
{
	private readonly string _logFilePath;
	private readonly object _lock = new();

	public FileLogger(string logFilePath)
	{
		_logFilePath = logFilePath;
		var directory = Path.GetDirectoryName(logFilePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
	}

	public void LogInfo(string message)
	{
		WriteLog("INFO", message);
	}

	public void LogDebug(string message)
	{
		WriteLog("DEBUG", message);
	}

	public void LogWarning(string message)
	{
		WriteLog("WARN", message);
	}

	public void LogError(string message)
	{
		WriteLog("ERROR", message);
	}

	private void WriteLog(string level, string message)
	{
		lock (_lock)
		{
			var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
			var logEntry = $"[{timestamp}] [{level}] {message}";
			File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
		}
	}
}
