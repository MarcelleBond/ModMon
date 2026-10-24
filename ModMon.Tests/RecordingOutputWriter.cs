using ModMon.Cli;

namespace ModMon.Tests;

internal sealed class RecordingOutputWriter : IOutputWriter
{
	public object? Result { get; private set; }

	public void WriteSuccess(object data)
	{
		Result = data;
	}

	public void WriteError(string message, object? details = null)
	{
		Result = details;
	}

	public void WriteProgress(string message)
	{
	}

	public void Flush()
	{
	}
}
