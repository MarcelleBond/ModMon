namespace ModMon.Cli;

internal sealed class DryRunDotnetCliRunner : IDotnetCliRunner
{
	private readonly IDryRunTracker _tracker;

	public DryRunDotnetCliRunner(IDryRunTracker tracker)
	{
		_tracker = tracker;
	}

	public Task<int> RunAsync(string arguments, string workingDirectory)
	{
		_tracker.RecordOperation(
			"dotnet-cli",
			new Dictionary<string, object>
			{
				["command"] = $"dotnet {arguments}",
				["workingDirectory"] = workingDirectory
			});
		return Task.FromResult(0);
	}
}
