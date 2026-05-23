namespace ModMon.Cli;

internal interface IConsole
{
	void WriteLine(string message);
	void WriteError(string message);
}
