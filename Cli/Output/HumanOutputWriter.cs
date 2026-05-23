using System.Text.Json;

namespace ModMon.Cli;

internal sealed class HumanOutputWriter : IOutputWriter
{
	private readonly IConsole _console;

	public HumanOutputWriter(IConsole console)
	{
		_console = console;
	}

	public void WriteSuccess(object data)
	{
		if (data is string message)
		{
			_console.WriteLine(message);
			return;
		}

		var json = JsonSerializer.Serialize(
			data,
			new JsonSerializerOptions { WriteIndented = true });
		_console.WriteLine(json);
	}

	public void WriteError(string message, object? details = null)
	{
		_console.WriteError(message);
	}

	public void WriteProgress(string message)
	{
		_console.WriteLine(message);
	}

	public void Flush()
	{
	}
}
