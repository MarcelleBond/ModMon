namespace ModMon.Cli;

internal interface IOutputWriter
{
	void WriteSuccess(object data);
	void WriteError(string message, object? details = null);
	void WriteProgress(string message);
	void Flush();
}
