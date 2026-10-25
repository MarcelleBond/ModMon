namespace ModMon.Cli;

internal static class DryRunServices
{
	public static IFileSystemWriter Wrap(
		IFileSystemWriter inner,
		IDryRunTracker tracker)
	{
		return tracker.IsDryRun
			? new DryRunFileSystemWriter(inner, tracker)
			: inner;
	}

	public static IDotnetCliRunner Wrap(
		IDotnetCliRunner inner,
		IDryRunTracker tracker)
	{
		return tracker.IsDryRun
			? new DryRunDotnetCliRunner(tracker)
			: inner;
	}
}
