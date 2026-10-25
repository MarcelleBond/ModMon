using ModMon.Cli;

namespace ModMon.Tests;

internal sealed class RecordingLogger : ILogger
{
	public List<string> Infos { get; } = new();
	public List<string> Debugs { get; } = new();
	public List<string> Warnings { get; } = new();
	public List<string> Errors { get; } = new();

	public void LogInfo(string message)
	{
		Infos.Add(message);
	}

	public void LogDebug(string message)
	{
		Debugs.Add(message);
	}

	public void LogWarning(string message)
	{
		Warnings.Add(message);
	}

	public void LogError(string message)
	{
		Errors.Add(message);
	}
}
