using ModMon.Cli;

namespace ModMon.Tests;

internal sealed class RecordingConsole : IConsole
{
	public List<string> Lines { get; } = new();
	public List<string> Errors { get; } = new();

	public void WriteLine(string message)
	{
		Lines.Add(message);
	}

	public void WriteError(string message)
	{
		Errors.Add(message);
	}
}
