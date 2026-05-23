namespace ModMon.Cli;

internal sealed class SystemConsole : IConsole
{
	public void WriteLine(string message)
	{
		Console.WriteLine(message);
	}

	public void WriteError(string message)
	{
		var current = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine(message);
		Console.ForegroundColor = current;
	}
}
