namespace ModMon.Cli;

internal sealed class StderrConsole : IConsole
{
	private readonly IConsole _inner;

	public StderrConsole(IConsole inner)
	{
		_inner = inner;
	}

	public void WriteLine(string message)
	{
		Console.Error.WriteLine(message);
	}

	public void WriteError(string message)
	{
		_inner.WriteError(message);
	}
}
