namespace ModMon.Cli;

internal interface IDotnetCliRunner
{
	Task<int> RunAsync(string arguments, string workingDirectory);
}
