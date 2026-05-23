namespace ModMon.Cli;

internal sealed class NullLogger : ILogger
{
	public void LogInfo(string message) { }
	public void LogDebug(string message) { }
	public void LogWarning(string message) { }
	public void LogError(string message) { }
}
